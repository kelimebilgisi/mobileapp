using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using CoreGraphics;
using Foundation;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Calendar;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using UIKit;
using Math = System.Math;

namespace Toggl.Daneel.Views.Calendar
{
    public sealed class CalendarCollectionViewLayout : UICollectionViewLayout
    {
        private const int hoursPerDay = 24;
        private float minHourHeight = 28;
        private float maxHourHeight = 28 * 4;

        public float HourHeight { get; private set; } = 56;

        private static readonly nfloat leftPadding = 76;
        private static readonly nfloat rightPadding = 16;
        private static readonly nfloat hourSupplementaryLabelHeight = 20;
        private static readonly nfloat currentTimeSupplementaryLeftOffset = -18;
        private static readonly nfloat horizontalItemSpacing = 4;
        private static readonly nfloat verticalItemSpacing = 1;

        private DateTime date;
        private readonly ITimeService timeService;
        private readonly ICalendarCollectionViewLayoutDataSource dataSource;

        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        public static NSString HourSupplementaryViewKind = new NSString("Hour");
        public static NSString EditingHourSupplementaryViewKind = new NSString("EditingHour");
        public static NSString CurrentTimeSupplementaryViewKind = new NSString("CurrentTime");
        public nfloat ContentViewHeight => hoursPerDay * HourHeight;

        private UICollectionViewLayoutAttributes currentTimeLayoutAttributes;

        private bool isEditing;
        public bool IsEditing
        {
            get => isEditing;
            set
            {
                isEditing = value;
                InvalidateLayoutForVisibleItems();
            }
        }

        public CalendarCollectionViewLayout(
            ITimeService timeService,
            ICalendarCollectionViewLayoutDataSource dataSource)
            : base()
        {
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));

            this.timeService = timeService;
            this.dataSource = dataSource;

            date = timeService.CurrentDateTime.ToLocalTime().Date;

            timeService
                .MidnightObservable
                .Subscribe(dateChanged)
                .DisposedBy(disposeBag);

            timeService
                .CurrentDateTimeObservable
                .DistinctUntilChanged(offset => offset.Minute)
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(_ => InvalidateCurrentTimeLayout())
                .DisposedBy(disposeBag);

            currentTimeLayoutAttributes = UICollectionViewLayoutAttributes.CreateForSupplementaryView(CurrentTimeSupplementaryViewKind, NSIndexPath.FromItemSection(0, 0));
        }

        public override void PrepareLayout()
        {
            base.PrepareLayout();

            minHourHeight = (float)CollectionView.Bounds.Height / 26;
            maxHourHeight = minHourHeight * 5;
        }

        public override CGSize CollectionViewContentSize
        {
            get
            {
                var width = CollectionView.Bounds.Width;
                var height = ContentViewHeight + hourSupplementaryLabelHeight;
                return new CGSize(width, height);
            }
        }

        public void ScaleHourHeight(nfloat scale, CGPoint basePoint)
        {
            Double newHourHeight = HourHeight * scale;

            if (newHourHeight >= minHourHeight && newHourHeight <= maxHourHeight)
            {
                var offset = basePoint.Y - basePoint.Y * scale;
                CollectionView.ContentOffset = new CGPoint(CollectionView.ContentOffset.X, CollectionView.ContentOffset.Y - offset);
            }

            HourHeight = (float)Math.Max(minHourHeight, Math.Min(maxHourHeight, newHourHeight));

            InvalidateLayout();
            InvalidateLayoutForVisibleItems();
            InvalidateCurrentTimeLayout();
        }

        public DateTimeOffset DateAtPoint(CGPoint point)
        {
            var seconds = (point.Y / HourHeight) * 60 * 60;
            var timespan = TimeSpan.FromSeconds(seconds);
            var nextDay = date.AddDays(1);

            var offset = date + timespan;

            if (offset < date)
                return date;
            if (offset > nextDay)
                return nextDay;

            return date + timespan;
        }

        public CGPoint PointAtDate(DateTimeOffset time)
            => new CGPoint(0, time.Hour * HourHeight + time.Minute / HourHeight);

        public void InvalidateLayoutForVisibleItems()
        {
            var context = new UICollectionViewLayoutInvalidationContext();
            context.InvalidateItems(CollectionView.IndexPathsForVisibleItems);
            context.InvalidateSupplementaryElements(EditingHourSupplementaryViewKind, indexPathsForEditingHours().ToArray());
            InvalidateLayout(context);
        }

        public void InvalidateCurrentTimeLayout()
        {
            var context = new UICollectionViewLayoutInvalidationContext();
            context.InvalidateSupplementaryElements(CurrentTimeSupplementaryViewKind, new NSIndexPath[] { NSIndexPath.FromItemSection(0, 0) });
            InvalidateLayout(context);
        }

        public override bool ShouldInvalidateLayoutForBoundsChange(CGRect newBounds)
            => true;

        public override UICollectionViewLayoutAttributes[] LayoutAttributesForElementsInRect(CGRect rect)
        {
            var eventsIndexPaths = indexPathsForVisibleItemsInRect(rect);
            var itemsAttributes = eventsIndexPaths.Select(LayoutAttributesForItem);

            var hoursIndexPaths = indexPathsForHoursInRect(rect);
            var hoursAttributes = hoursIndexPaths.Select(layoutAttributesForHourView);

            var editingHoursIndexPaths = indexPathsForEditingHours();
            var editingHoursAttributes = editingHoursIndexPaths.Select(layoutAttributesForHourView);

            var attributes = itemsAttributes
                .Concat(hoursAttributes)
                .Concat(editingHoursAttributes)
                .Append(currentTimeLayoutAttributes);

            return attributes.ToArray();
        }

        public override UICollectionViewLayoutAttributes LayoutAttributesForItem(NSIndexPath indexPath)
        {
            var calendarItemLayoutAttributes = dataSource.LayoutAttributesForItemAtIndexPath(indexPath);

            var attributes = UICollectionViewLayoutAttributes.CreateForCell(indexPath);
            attributes.Frame = frameForItemWithLayoutAttributes(calendarItemLayoutAttributes);
            attributes.ZIndex = zIndexForItemAtIndexPath(indexPath);

            return attributes;
        }

        public override UICollectionViewLayoutAttributes LayoutAttributesForSupplementaryView(NSString kind, NSIndexPath indexPath)
        {
            if (kind == HourSupplementaryViewKind)
            {
                var attributes = UICollectionViewLayoutAttributes.CreateForSupplementaryView(kind, indexPath);
                attributes.Frame = frameForHour((int)indexPath.Item);
                attributes.ZIndex = 0;
                return attributes;
            }
            else if (kind == EditingHourSupplementaryViewKind)
            {
                var attributes = UICollectionViewLayoutAttributes.CreateForSupplementaryView(kind, indexPath);
                attributes.Frame = frameForEditingHour(indexPath);
                attributes.ZIndex = 200;
                return attributes;
            }
            else
            {
                currentTimeLayoutAttributes.Frame = FrameForCurrentTime();
                currentTimeLayoutAttributes.ZIndex = 300;
                return currentTimeLayoutAttributes;
            }
        }

        internal CGRect FrameForCurrentTime()
        {
            var now = timeService.CurrentDateTime.LocalDateTime;

            var yHour = HourHeight * now.Hour;
            var yMins = HourHeight * now.Minute / 60;

            var width = CollectionViewContentSize.Width - leftPadding - rightPadding - currentTimeSupplementaryLeftOffset;
            var height = 8;
            var x = leftPadding + currentTimeSupplementaryLeftOffset;
            var y = yHour + yMins - height / 2;

            return new CGRect(x, y, width, height);
        }

        private nfloat minItemHeight()
            => HourHeight / 4;

        private nint zIndexForItemAtIndexPath(NSIndexPath indexPath)
        {
            var editingIndexIndexPath = dataSource.IndexPathForEditingItem();
            var isEditing = editingIndexIndexPath != null && editingIndexIndexPath.Item == indexPath.Item;
            return isEditing ? 150 : 100;
        }

        private UICollectionViewLayoutAttributes layoutAttributesForHourView(NSIndexPath indexPath)
            => LayoutAttributesForSupplementaryView(HourSupplementaryViewKind, indexPath);

        private IEnumerable<NSIndexPath> indexPathsForVisibleItemsInRect(CGRect rect)
        {
            var minHour = (int)Math.Floor(rect.GetMinY() / HourHeight).Clamp(0, hoursPerDay);
            var maxHour = (int)Math.Floor(rect.GetMaxY() / HourHeight).Clamp(0, hoursPerDay);

            return dataSource.IndexPathsOfCalendarItemsBetweenHours(minHour, maxHour);
        }

        private IEnumerable<NSIndexPath> indexPathsForHoursInRect(CGRect rect)
        {
            var minHour = (int)Math.Floor(rect.GetMinY() / HourHeight).Clamp(0, hoursPerDay);
            var maxHour = (int)Math.Floor(rect.GetMaxY() / HourHeight).Clamp(0, hoursPerDay + 1);

            return Enumerable
                .Range(minHour, maxHour - minHour)
                .Select(hour => NSIndexPath.FromItemSection(hour, 0))
                .ToArray();
        }

        private IEnumerable<NSIndexPath> indexPathsForEditingHours()
        {
            if (IsEditing)
            {
                var editingItemIndexPath = dataSource.IndexPathForEditingItem();
                var runningTimeEntryIndexPath = dataSource.IndexPathForRunningTimeEntry();
                var isEditingRunningTimeEntry = editingItemIndexPath != null && runningTimeEntryIndexPath != null
                                                && runningTimeEntryIndexPath.Item == editingItemIndexPath.Item;
                return isEditingRunningTimeEntry
                    ? new NSIndexPath[] { NSIndexPath.FromItemSection(0, 0) }
                    : new NSIndexPath[] { NSIndexPath.FromItemSection(0, 0), NSIndexPath.FromItemSection(1, 0) };
            }
            return Enumerable.Empty<NSIndexPath>();
        }

        private CGRect frameForItemWithLayoutAttributes(CalendarItemLayoutAttributes attrs)
        {
            var startTime = attrs.StartTime < date ? date : attrs.StartTime;
            var endTime = attrs.EndTime > date.AddDays(1) ? date.AddDays(1) : attrs.EndTime;
            var duration = endTime - startTime;

            var yHour = HourHeight * startTime.Hour;
            var yMins = HourHeight * startTime.Minute / 60;

            var totalInterItemSpacing = (attrs.TotalColumns - 1) * horizontalItemSpacing;
            var width = (CollectionViewContentSize.Width - leftPadding - rightPadding - totalInterItemSpacing) / attrs.TotalColumns;
            var height = Math.Max(minItemHeight(), HourHeight * duration.TotalMinutes / 60) - verticalItemSpacing;
            var x = leftPadding + (width + horizontalItemSpacing) * attrs.ColumnIndex;
            var y = yHour + yMins + verticalItemSpacing;

            return new CGRect(x, y, width, height);
        }

        private CGRect frameForHour(int hour)
        {
            var width = CollectionViewContentSize.Width - rightPadding;
            var height = hourSupplementaryLabelHeight;
            var x = 0;
            var y = HourHeight * hour - height / 2;

            return new CGRect(x, y, width, height);
        }

        private CGRect frameForEditingHour(NSIndexPath indexPath)
        {
            var editingItemIndexPath = dataSource.IndexPathForEditingItem();
            var attrs = dataSource.LayoutAttributesForItemAtIndexPath(editingItemIndexPath);

            var time = (int)indexPath.Item == 0 ? attrs.StartTime : attrs.EndTime;
            var yHour = HourHeight * time.Hour;
            var yMins = HourHeight * time.Minute / 60;

            var width = CollectionViewContentSize.Width - rightPadding;
            var height = hourSupplementaryLabelHeight;
            var x = 0;
            var y = yHour + yMins - height / 2;

            return new CGRect(x, y, width, height);
        }

        private void dateChanged(DateTimeOffset dateTimeOffset)
        {
            date = dateTimeOffset.ToLocalTime().Date;
            InvalidateLayout();
        }
    }
}
