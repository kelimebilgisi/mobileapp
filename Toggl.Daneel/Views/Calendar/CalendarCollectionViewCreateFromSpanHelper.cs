using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreGraphics;
using Foundation;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation.Helper;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.Views.Calendar
{
    public sealed class CalendarCollectionViewCreateFromSpanHelper : CalendarCollectionViewAutoScrollHelper, IUIGestureRecognizerDelegate
    {
        private static readonly TimeSpan defaultDuration = Constants.CalendarItemViewDefaultDuration;

        private readonly CalendarCollectionViewSource dataSource;

        private UILongPressGestureRecognizer longPressGestureRecognizer;

        private NSIndexPath itemIndexPath;

        private CGPoint firstPoint;

        private TimeSpan? previousDuration;

        private readonly ISubject<(DateTimeOffset, TimeSpan)> createFromSpanSuject = new Subject<(DateTimeOffset, TimeSpan)>();

        public IObservable<(DateTimeOffset, TimeSpan)> CreateFromSpan => createFromSpanSuject.AsObservable();

        private List<DateTimeOffset> allItemsStartAndEndTime;

        public CalendarCollectionViewCreateFromSpanHelper(
            UICollectionView collectionView,
            CalendarCollectionViewSource dataSource,
            CalendarCollectionViewLayout Layout) : base(collectionView, Layout)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));

            this.dataSource = dataSource;

            longPressGestureRecognizer = new UILongPressGestureRecognizer(onLongPress);
            longPressGestureRecognizer.Delegate = this;
            collectionView.AddGestureRecognizer(longPressGestureRecognizer);
        }

        public CalendarCollectionViewCreateFromSpanHelper(IntPtr handle) : base(handle)
        {
        }

        [Export("gestureRecognizer:shouldRecognizeSimultaneouslyWithGestureRecognizer:")]
        public bool ShouldRecognizeSimultaneously(UIGestureRecognizer gestureRecognizer, UIGestureRecognizer otherGestureRecognizer)
        {
            if (gestureRecognizer == longPressGestureRecognizer)
                return otherGestureRecognizer is UILongPressGestureRecognizer;
            else
                return false;
        }

        [Export("gestureRecognizer:shouldReceiveTouch:")]
        public bool ShouldReceiveTouch(UIGestureRecognizer gestureRecognizer, UITouch touch)
        {
            if (gestureRecognizer == longPressGestureRecognizer)
            {
                var point = touch.LocationInView(CollectionView);
                var thereIsNoItemAtPoint = dataSource.CalendarItemAtPoint(point) == null;
                var isNotEditing = dataSource.IsEditing == false;
                return thereIsNoItemAtPoint && isNotEditing;
            }

            return true;
        }

        private void onLongPress(UILongPressGestureRecognizer gesture)
        {
            var point = gesture.LocationInView(CollectionView);
            var isEditing = dataSource.IsEditing && itemIndexPath != null;

            switch (gesture.State)
            {
                case UIGestureRecognizerState.Began when !dataSource.IsEditing && dataSource.CalendarItemAtPoint(point) == null:
                    longPressBegan(point);
                    break;

                case UIGestureRecognizerState.Changed when isEditing:
                    longPressChanged(point);
                    break;

                case UIGestureRecognizerState.Ended when isEditing:
                    longPressEnded(point);
                    break;

                case UIGestureRecognizerState.Failed:
                    itemIndexPath = null;
                    break;

                case UIGestureRecognizerState.Cancelled:
                    if (itemIndexPath != null)
                    {
                        dataSource.RemoveItemView(itemIndexPath);
                        dataSource.StopEditing();
                        itemIndexPath = null;
                    }

                    break;
            }
        }

        private void longPressBegan(CGPoint point)
        {
            allItemsStartAndEndTime = dataSource.AllItemsStartAndEndTime();

            dataSource.StartEditing();
            firstPoint = point;
            LastPoint = point;
            var startTime = Layout.DateAtPoint(firstPoint).RoundDownToClosestQuarter();
            itemIndexPath = dataSource.InsertItemView(startTime, defaultDuration);
            impactFeedback.ImpactOccurred();
            selectionFeedback.Prepare();
            previousDuration = defaultDuration;
        }

        private bool isDraggingDown(CGPoint point) => firstPoint.Y < point.Y;

        private void longPressChanged(CGPoint point)
        {
            LastPoint = point;

            DateTimeOffset startTime;
            DateTimeOffset endTime;

            if (isDraggingDown(point))
            {
                startTime = Layout.DateAtPoint(firstPoint).RoundDownToClosestQuarter();
                endTime = NewEndTimeWithDynamicDuration(point, allItemsStartAndEndTime);

                if (point.Y > BottomAutoScrollLine)
                    StartAutoScrolDown(longPressChanged);
                else
                    StopAutoScroll();
            }
            else
            {
                startTime = NewStartTimeWithDynamicDuration(point, allItemsStartAndEndTime);
                endTime = Layout.DateAtPoint(firstPoint).RoundDownToClosestQuarter();

                if (point.Y < TopAutoScrollLine)
                    StartAutoScrollUp(longPressChanged);
                else
                    StopAutoScroll();
            }

            var duration = endTime - startTime;

            dataSource.UpdateItemView(itemIndexPath, startTime, duration);

            if (duration != previousDuration)
            {
                selectionFeedback.SelectionChanged();
                previousDuration = duration;
            }
        }

        private void longPressEnded(CGPoint point)
        {
            previousDuration = null;
            LastPoint = point;

            DateTimeOffset startTime;
            DateTimeOffset endTime;

            if (firstPoint.Y < point.Y)
            {
                startTime = Layout.DateAtPoint(firstPoint).RoundDownToClosestQuarter();
                endTime = Layout.DateAtPoint(LastPoint).RoundUpToClosestQuarter();
            }
            else
            {
                startTime = Layout.DateAtPoint(LastPoint).RoundDownToClosestQuarter();
                endTime = Layout.DateAtPoint(firstPoint).RoundDownToClosestQuarter();
            }

            var duration = endTime - startTime;
            createFromSpanSuject.OnNext((startTime, duration));

            dataSource.StopEditing();
            itemIndexPath = null;
            StopAutoScroll();

            impactFeedback.ImpactOccurred();
        }
    }
}
