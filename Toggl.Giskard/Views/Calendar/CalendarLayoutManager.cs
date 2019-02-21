using System;
using System.Collections.Generic;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Foundation.Helper;
using Toggl.Giskard.Adapters.Calendar;

namespace Toggl.Giskard.Views.Calendar
{
    public partial class CalendarLayoutManager : RecyclerView.LayoutManager
    {
        private const int anchorCount = Constants.HoursPerDay;
        private OrientationHelper orientationHelper;
        private AnchorInfo anchorInfo;
        private LayoutState layoutState;
        private LayoutChunkResult layoutChunkResult;

        public CalendarLayoutManager()
        {
            orientationHelper = OrientationHelper.CreateVerticalHelper(this);
            anchorInfo = new AnchorInfo(orientationHelper, anchorCount);
            layoutState = new LayoutState(anchorCount);
        }

        public override RecyclerView.LayoutParams GenerateDefaultLayoutParams()
        {
            return new RecyclerView.LayoutParams(RecyclerView.LayoutParams.WrapContent, RecyclerView.LayoutParams.WrapContent);
        }

        public override bool CanScrollVertically() => true;

        public override bool CanScrollHorizontally() => false;

        public override void OnLayoutChildren(RecyclerView.Recycler recycler, RecyclerView.State state)
        {
            layoutState.Recycle = false;


            if (!anchorInfo.IsValid)
            {
                //todo: try restoring anchor info from saved state;
                anchorInfo.Reset();
                updateAnchorInfoForLayout();
                anchorInfo.IsValid = true;
            }

            //todo: handle extras needed to support scrollTo

            var extraForStart = orientationHelper.StartAfterPadding;
            var extraForEnd = orientationHelper.EndPadding;

            //todo: layout extra children to support nice animations if pending scroll position exists

            var startOffset = 0;
            var endOffset = 0;

            DetachAndScrapAttachedViews(recycler);

            layoutState.IsPreLayout = state.IsPreLayout;

            //fill towards the end
            updateLayoutStateToFillEnd();
            layoutState.Extra = extraForEnd;
            fill(recycler, state);
            //todo: fill towards the start when we handle saving state
            //todo: fix possible gaps

            if (state.IsPreLayout)
            {
                orientationHelper.OnLayoutComplete();
            }
            else
            {
                anchorInfo.Reset();
            }
        }

        public override void OnLayoutCompleted(RecyclerView.State state)
        {
            base.OnLayoutCompleted(state);
            anchorInfo.Reset();
        }

        // don't scroll horizontally
        public override int ScrollHorizontallyBy(int dx, RecyclerView.Recycler recycler, RecyclerView.State state)
            => 0;

        public override int ScrollVerticallyBy(int dy, RecyclerView.Recycler recycler, RecyclerView.State state)
        {
            return scrollBy(dy, recycler, state);
        }

        public override int ComputeVerticalScrollOffset(RecyclerView.State state)
        {
            return computeScrollOffset(state,
                findFirstVisibleChildClosestToStart(),
                findFirstVisibleChildClosestToEnd()
            );
        }

        public override int ComputeVerticalScrollRange(RecyclerView.State state)
        {
            //todo: calculate the of the whole calendar (anchor height * anchor count)
            return base.ComputeVerticalScrollRange(state);
        }

        public override int ComputeVerticalScrollExtent(RecyclerView.State state)
        {
            //todo: calculate the space taken to fill the available space in the screen with anchors
            return base.ComputeVerticalScrollExtent(state);
        }

        private View findFirstVisibleChildClosestToStart()
        {
            return findFirstVisibleChildInRange(0, ChildCount);
        }

        private View findFirstVisibleChildClosestToEnd()
        {
            return findFirstVisibleChildInRange(ChildCount - 1, -1);
        }

        private View findFirstVisibleChildInRange(int fromIndex, int toIndex)
        {
            //preferred -> child start >= parent start && child end <= parent end (completely visible)
            //acceptable -> child start < parent end && child end > parent start (partially visible)
            View acceptableMatch = null;
            var parentStart = PaddingTop;
            var parentEnd = Height - PaddingBottom;

            var next = toIndex > fromIndex ? 1 : -1;

            for (int i = fromIndex; i != toIndex; i += next)
            {
                var child = GetChildAt(i);
                if (!isAnchor(child)) continue;

                var childStart = getChildStart(child);
                var childEnd = getChildEnd(child);

                if (childStart >= parentStart && childEnd <= parentEnd)
                {
                    //perfect match
                    return child;
                }

                if (childStart < parentEnd && childEnd > parentStart)
                {
                    acceptableMatch = child;
                }
            }

            return acceptableMatch;
        }

        private int getChildStart(View view)
        {
            var layoutParams = (RecyclerView.LayoutParams) view.LayoutParameters;
            return GetDecoratedTop(view) - layoutParams.TopMargin;
        }

        private int getChildEnd(View view)
        {
            var layoutParams = (RecyclerView.LayoutParams) view.LayoutParameters;
            return GetDecoratedBottom(view) + layoutParams.BottomMargin;
        }

        private int fill(RecyclerView.Recycler recycler, RecyclerView.State state)
        {
            //todo: handle focusable
            var start = layoutState.Available;
            if (layoutState.ScrollingOffset != INVALID_SCROLLING_OFFSET)
            {
                if (layoutState.Available < 0)
                {
                    layoutState.ScrollingOffset += layoutState.Available;
                }

                recycleByLayoutState(recycler);
            }

            var remainingSpace = layoutState.Available + layoutState.Extra;

            while (remainingSpace > 0 && layoutState.HasMore())
            {
                layoutChunkResult.ResetInternal();

                layoutChunk(recycler, state);

                if (layoutChunkResult.IsFinished)
                {
                    break;
                }

                layoutState.Offset += layoutChunkResult.Consumed * layoutState.LayoutDirection;

                // layoutChunk didn't request to be ignored Or We are laying  out scrap children Or Not doing pre-layout
                if (!layoutChunkResult.IgnoreConsumed || layoutState.ScrapList != null || !state.IsPreLayout)
                {
                    layoutState.Available -= layoutChunkResult.Consumed;
                    //important for recycling
                    remainingSpace -= layoutChunkResult.Consumed;
                }

                if (layoutState.ScrollingOffset != INVALID_SCROLLING_OFFSET)
                {
                    layoutState.ScrollingOffset += layoutChunkResult.Consumed;
                    if (layoutState.Available < 0)
                    {
                        layoutState.ScrollingOffset += layoutState.Available;
                    }

                    recycleByLayoutState(recycler);
                }

                //todo: handle focusable view logic (stop fill when you find a focusable view)
            }

            //todo: maybe layout anchored views here?
            return start - layoutState.Available;
        }

        private void layoutChunk(RecyclerView.Recycler recycler, RecyclerView.State state)
        {
            var view = layoutState.Next(recycler);
            if (view == null || !(view.LayoutParameters is RecyclerView.LayoutParams layoutParams))
            {
                layoutChunkResult.IsFinished = true;
                return;
            }

            //todo: check for scrap list if we do predictive animations
            if (layoutState.LayoutDirection == TOWARDS_THE_END)
                AddView(view);
            else
                AddView(view, 0);

            if (!(view.Tag is Anchor anchor)) return;

            MeasureChildWithMargins(view, 0, anchor.Height);
            layoutChunkResult.Consumed = anchor.Height;

            var anchorLeft = PaddingLeft;
            var anchorRight = anchorLeft + view.MeasuredWidth;

            int anchorTop;
            int anchorBottom;

            if (layoutState.LayoutDirection == TOWARDS_THE_START)
            {
                anchorBottom = layoutState.Offset;
                anchorTop = layoutState.Offset - layoutChunkResult.Consumed;
            }
            else
            {
                anchorTop = layoutState.Offset;
                anchorBottom = layoutState.Offset + layoutChunkResult.Consumed;
            }

            LayoutDecoratedWithMargins(view, anchorLeft, anchorTop, anchorRight, anchorBottom);

            if (layoutParams.IsItemRemoved || layoutParams.IsItemChanged)
            {
                layoutChunkResult.IgnoreConsumed = true;
            }

            foreach (var anchorData in anchor.AnchoredData)
            {
                var anchoredView = recycler.GetViewForPosition(anchorData.adapterPosition);
                var anchoredViewLeft = anchorLeft + anchorData.leftOffset;
                var anchoredViewTop = anchorTop + anchorData.topOffset;

                if (layoutState.LayoutDirection == TOWARDS_THE_END)
                {
                    AddView(anchoredView);
                }
                else
                {
                    AddView(anchoredView, 0);
                }

                MeasureChildWithMargins(anchoredView, anchorData.width, anchorData.height);
                LayoutDecoratedWithMargins(anchoredView,
                    anchoredViewLeft,
                    anchoredViewTop,
                    anchoredViewLeft + anchorData.width,
                    anchoredViewTop + anchorData.height);
            }
        }

        private void updateAnchorInfoForLayout()
        {
            if (tryUpdateAnchorInfoFromChildren()) return;

            anchorInfo.AssignCoordinateFromPadding();
            anchorInfo.Position = 0;
        }

        private bool tryUpdateAnchorInfoFromChildren()
        {
            if (ChildCount == 0)
                return false;

            var referenceChild = findReferenceChildClosestToStart();
            if (referenceChild == null)
                return false;

            anchorInfo.AssignFromView(referenceChild, GetPosition(referenceChild));
            return true;
        }

        private View findReferenceChildClosestToStart()
        {
            View invalidMatch = null;
            View matchOutOfBounds = null;
            var boundsStart = orientationHelper.StartAfterPadding;
            var boundsEnd = orientationHelper.EndAfterPadding;
            var currentChildCount = ChildCount;

            bool isOutOfBounds(View view)
            {
                return orientationHelper.GetDecoratedStart(view) >= boundsStart
                       || orientationHelper.GetDecoratedEnd(view) < boundsEnd;
            }

            for (int i = 0; i < currentChildCount; i++)
            {
                var candidate = GetChildAt(i);
                if (candidate == null) continue;

                var candidatePosition = GetPosition(candidate);
                if (candidatePosition >= 0 && candidatePosition < anchorCount)
                {
                    var layoutParams = candidate.LayoutParameters as RecyclerView.LayoutParams;
                    if (invalidMatch == null && layoutParams.IsItemRemoved)
                    {
                        invalidMatch = candidate;
                    }
                    else if (matchOutOfBounds == null && isOutOfBounds(candidate))
                    {
                        matchOutOfBounds = candidate;
                    }
                    else
                    {
                        return candidate;
                    }
                }
            }

            return matchOutOfBounds ?? invalidMatch;
        }

        private void updateLayoutStateToFillEnd()
        {
            layoutState.Offset = anchorInfo.Coordinate;
            layoutState.Available = orientationHelper.EndAfterPadding - anchorInfo.Coordinate;
            layoutState.CurrentAnchorPosition = anchorInfo.Position;
            layoutState.ItemDirection = TOWARDS_THE_END;
            layoutState.LayoutDirection = TOWARDS_THE_END;
            layoutState.ScrollingOffset = INVALID_SCROLLING_OFFSET;
        }

        private void recycleByLayoutState(RecyclerView.Recycler recycler)
        {
            if (layoutState.Recycle)
            {
                if (layoutState.LayoutDirection == TOWARDS_THE_START)
                {
                    recycleViewsFromEnd(recycler, layoutState.ScrollingOffset);
                }
                else
                {
                    recycleViewsFromStart(recycler, layoutState.ScrollingOffset);
                }
            }
        }

        private void recycleViewsFromStart(RecyclerView.Recycler recycler, int scrollingOffset)
        {
            if (scrollingOffset < 0) return;

            var currentChildCount = ChildCount;

            for (var i = 0; i < currentChildCount; i++)
            {
                var child = GetChildAt(i);
                if (orientationHelper.GetDecoratedEnd(child) > scrollingOffset || orientationHelper.GetTransformedEndWithDecoration(child) > scrollingOffset)
                {
                    recycleChildren(recycler, 0, i);
                    return;
                }
            }
        }

        private void recycleViewsFromEnd(RecyclerView.Recycler recycler, int scrollingOffset)
        {
            if (scrollingOffset < 0) return;
            var limit = orientationHelper.End - scrollingOffset;
            var currentChildCount = ChildCount;
            for (var i = currentChildCount - 1; i >= 0; i--)
            {
                var child = GetChildAt(i);
                if (orientationHelper.GetDecoratedStart(child) < limit || orientationHelper.GetTransformedStartWithDecoration(child) < limit)
                {
                    recycleChildren(recycler, currentChildCount - 1, i);
                    return;
                }
            }
        }

        private void recycleChildren(RecyclerView.Recycler recycler, int startIndex, int endIndex)
        {
            if (startIndex == endIndex) return;

            if (endIndex > startIndex)
            {
                for (var i = endIndex - 1; i >= startIndex; i--)
                    RemoveAndRecycleViewAt(i, recycler);
            }
            else
            {
                for (var i = startIndex; i > endIndex; i--)
                    RemoveAndRecycleViewAt(i, recycler);
            }
        }

        private int scrollBy(int dy, RecyclerView.Recycler recycler, RecyclerView.State state)
        {
            if (ChildCount == 0 || dy == 0)
                return 0;

            layoutState.Recycle = true;

            var layoutDirection = dy > 0 ? TOWARDS_THE_END : TOWARDS_THE_START;
            var absDy = Math.Abs(dy);

            updateLayoutState(layoutDirection, absDy, true, state);

            var consumed = layoutState.ScrollingOffset + fill(recycler, state);

            if (consumed < 0) return 0;

            var scrolled = absDy > consumed
                ? layoutDirection * consumed
                : dy;

            orientationHelper.OffsetChildren(-scrolled);
            layoutState.LastScrollDelta = scrolled;

            return scrolled;
        }

        private void updateLayoutState(int layoutDirection, int requiredSpace, bool canUseExistingSpace, RecyclerView.State state)
        {
            layoutState.Extra = getExtraLayoutSpace(state);
            layoutState.LayoutDirection = layoutDirection;

            int scrollingOffset;
            if (layoutDirection == TOWARDS_THE_END)
            {
                var child = getChildClosestToEnd();
                if (child == null) return;
                layoutState.Extra += orientationHelper.EndPadding;
                layoutState.ItemDirection = layoutDirection;
                layoutState.CurrentAnchorPosition = GetPosition(child) + layoutState.ItemDirection;
                layoutState.Offset = orientationHelper.GetDecoratedEnd(child);
                scrollingOffset = orientationHelper.GetDecoratedEnd(child) - orientationHelper.EndAfterPadding;
            }
            else
            {
                var child = getChildClosestToStart();
                if (child == null) return;
                layoutState.Extra += orientationHelper.StartAfterPadding;
                layoutState.ItemDirection = layoutDirection;
                layoutState.CurrentAnchorPosition = GetPosition(child) + layoutState.ItemDirection;
                layoutState.Offset = orientationHelper.GetDecoratedStart(child);
                scrollingOffset = -orientationHelper.GetDecoratedStart(child) + orientationHelper.StartAfterPadding;
            }

            layoutState.Available = requiredSpace;
            if (canUseExistingSpace)
            {
                layoutState.Available -= scrollingOffset;
            }

            layoutState.ScrollingOffset = scrollingOffset;
        }

        private View getChildClosestToStart()
        {
            for (var i = 0; i < ChildCount; i++)
            {
                var candidate = GetChildAt(i);
                if (isAnchor(candidate)) return candidate;
            }

            return null;
        }

        private View getChildClosestToEnd()
        {
            for (var i = ChildCount - 1; i >= 0; i--)
            {
                var candidate = GetChildAt(i);
                if (isAnchor(candidate)) return candidate;
            }

            return null;
        }

        private bool isAnchor(View view)
        {
            return view?.Tag is Anchor;
        }

        private int getExtraLayoutSpace(RecyclerView.State state)
        {
            return state.HasTargetScrollPosition ? orientationHelper.TotalSpace : 0;
        }

        private struct LayoutChunkResult
        {
            public int Consumed { get; set; }
            public bool IsFinished { get; set; }
            public bool IgnoreConsumed { get; set; }

            public void ResetInternal()
            {
                Consumed = 0;
                IsFinished = false;
                IgnoreConsumed = false;
            }
        }
    }
}
