using System;
using System.Collections.Generic;
using System.Linq;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using Toggl.Daneel.Extensions;
using Toggl.Multivac;
using UIKit;
using Math = System.Math;
using Toggl.Multivac.Extensions;

namespace Toggl.Daneel.Views.Calendar
{
    public abstract class CalendarCollectionViewAutoScrollHelper : NSObject
    {
        protected UICollectionView CollectionView { get; }
        protected CalendarCollectionViewLayout Layout { get; }

        protected CGPoint LastPoint { get; set; }

        protected float TopAutoScrollLine => (float)CollectionView.ContentOffset.Y + autoScrollZoneHeight;
        protected float BottomAutoScrollLine => (float)(CollectionView.ContentOffset.Y + CollectionView.Frame.Height - autoScrollZoneHeight);

        private bool shouldStopAutoScrollUp => isAutoScrollingUp && CollectionView.IsAtTop();
        private bool shouldStopAutoScrollDown => isAutoScrollingDown && CollectionView.IsAtBottom();

        private bool isAutoScrollingUp;
        private bool isAutoScrollingDown;
        private CADisplayLink displayLink;

        private const int autoScrollZoneHeight = 50;
        private const int autoScrollsPerSecond = 8;
        private readonly float autoScrollAmount;

        protected UISelectionFeedbackGenerator selectionFeedback = new UISelectionFeedbackGenerator();
        protected UIImpactFeedbackGenerator impactFeedback = new UIImpactFeedbackGenerator(UIImpactFeedbackStyle.Medium);

        protected CalendarCollectionViewAutoScrollHelper(
            UICollectionView collectionView,
            CalendarCollectionViewLayout layout)
        {
            Ensure.Argument.IsNotNull(layout, nameof(layout));
            Ensure.Argument.IsNotNull(collectionView, nameof(collectionView));

            Layout = layout;
            CollectionView = collectionView;

            autoScrollAmount = Layout.HourHeight / 4;
        }

        public CalendarCollectionViewAutoScrollHelper(IntPtr handle) : base(handle)
        {
        }

        protected void StartAutoScrollUp(Action<CGPoint> updateEntryAction)
        {
            isAutoScrollingUp = true;
            if (displayLink == null)
                createDisplayLinkWithScrollAmount(-autoScrollAmount - 10, updateEntryAction);
        }

        protected void StartAutoScrolDown(Action<CGPoint> updateEntryAction)
        {
            isAutoScrollingDown = true;
            if (displayLink == null)
                createDisplayLinkWithScrollAmount(autoScrollAmount, updateEntryAction);
        }

        protected void StopAutoScroll()
        {
            if (displayLink == null) return;

            displayLink.Paused = true;
            displayLink.Dispose();
            displayLink = null;
            isAutoScrollingUp = false;
            isAutoScrollingDown = false;
        }

        protected DateTimeOffset NewStartTimeWithDynamicDuration(CGPoint point, List<DateTimeOffset> allItemsStartAndEndTime)
        {
            if (!allItemsStartAndEndTime.Any())
            {
                return Layout.DateAtPoint(point).ToLocalTime().RoundToClosestQuarter();
            }

            var newStartTime = Layout.DateAtPoint(point).ToLocalTime();

            var startSnappingPointDifference = distanceToClosestSnappingPoint(newStartTime, allItemsStartAndEndTime.Append(newStartTime.RoundToClosestQuarter()));

            newStartTime += startSnappingPointDifference;

            return newStartTime;
        }

        protected DateTimeOffset NewEndTimeWithDynamicDuration(CGPoint point, List<DateTimeOffset> allItemsStartAndEndTime)
        {
            if (!allItemsStartAndEndTime.Any())
            {
                return Layout.DateAtPoint(point).ToLocalTime().RoundToClosestQuarter();
            }

            var newEndTime = Layout.DateAtPoint(point).ToLocalTime();

            var endSnappingPointDifference = distanceToClosestSnappingPoint((DateTimeOffset)newEndTime, allItemsStartAndEndTime.Append(newEndTime.RoundToClosestQuarter()));

            newEndTime += endSnappingPointDifference;

            return newEndTime;
        }

        protected DateTimeOffset NewStartTimeWithStaticDuration(CGPoint point, List<DateTimeOffset> allItemsStartAndEndTime, TimeSpan? duration)
        {
            if (!allItemsStartAndEndTime.Any())
            {
                return Layout.DateAtPoint(point).ToLocalTime().RoundToClosestQuarter();
            }

            var newStartTime = Layout.DateAtPoint(point).ToLocalTime();
            var newEndTime = duration.HasValue ? newStartTime + duration : null;

            var startSnappingPointDifference = distanceToClosestSnappingPoint(newStartTime, allItemsStartAndEndTime.Append(newStartTime.RoundToClosestQuarter()));

            if (newEndTime.HasValue)
            {
                var endSnappingPointDifference = distanceToClosestSnappingPoint((DateTimeOffset)newEndTime, allItemsStartAndEndTime);
                var snappingPointDifference = startSnappingPointDifference.Positive() < endSnappingPointDifference.Positive() ? startSnappingPointDifference : endSnappingPointDifference;
                newStartTime += snappingPointDifference;
            }
            else
            {
                newStartTime += startSnappingPointDifference;
            }

            return newStartTime;
        }

        private TimeSpan distanceToClosestSnappingPoint(DateTimeOffset time, IEnumerable<DateTimeOffset> data)
            => data.Aggregate(TimeSpan.MaxValue,
                (min, next) => min.Positive() <= (next - time).Positive()
                    ? min
                    : (next - time));

        private void createDisplayLinkWithScrollAmount(float scrollAmount, Action<CGPoint> updateEntryAction)
        {
            displayLink = CADisplayLink.Create(() =>
            {
                if (shouldStopAutoScrollUp || shouldStopAutoScrollDown)
                {
                    StopAutoScroll();
                    return;
                }

                var point = LastPoint;
                point.Y += scrollAmount;
                var targetOffset = CollectionView.ContentOffset;
                targetOffset.Y += scrollAmount;
                CollectionView.SetContentOffset(targetOffset, false);
                updateEntryAction(point);
            });

            displayLink.PreferredFramesPerSecond = autoScrollsPerSecond;
            displayLink.AddToRunLoop(NSRunLoop.Current, NSRunLoopMode.Default);
        }
    }
}
