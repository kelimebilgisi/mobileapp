using System;
using System.Collections.Generic;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Toggl.Foundation.Helper;
using Toggl.Giskard.Adapters.Calendar;

namespace Toggl.Giskard.Views.Calendar
{
    public partial class CalendarLayoutManager : RecyclerView.LayoutManager
    {
        private const int anchorCount = Constants.HoursPerDay;

        private readonly OrientationHelper orientationHelper;

        private AnchorInfo anchorInfo;
        private LayoutState layoutState;
        private LayoutChunkResult layoutChunkResult;
        private List<View> anchors = new List<View>();
        private List<View> anchoredViews = new List<View>();
        private SparseArray<View> anchoredViewsPositions = new SparseArray<View>();

        private AnchorSavedState pendingAnchorSavedState;
        private int pendingScrollPosition = RecyclerView.NoPosition;
        private int pendingScrollPositionOffset = InvalidOffset;

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
            if (pendingAnchorSavedState != null || pendingScrollPosition != RecyclerView.NoPosition)
            {
                if (state.ItemCount == 0)
                {
                    RemoveAndRecycleAllViews(recycler);
                    return;
                }
            }

            if (pendingAnchorSavedState != null && pendingAnchorSavedState.HasValidAnchor())
            {
                pendingScrollPosition = pendingAnchorSavedState.TopAnchorPosition;
            }

            layoutState.Recycle = false;

            if (!anchorInfo.IsValid || pendingScrollPosition != RecyclerView.NoPosition || pendingAnchorSavedState != null)
            {
                anchorInfo.Reset();
                updateAnchorInfoForLayout(state);
                anchorInfo.IsValid = true;
            }

            int extraForStart;
            int extraForEnd;
            var extra = getExtraLayoutSpace(state);

            if (layoutState.LastScrollDelta >= 0)
            {
                extraForEnd = extra;
                extraForStart = 0;
            }
            else
            {
                extraForStart = extra;
                extraForEnd = 0;
            }

            extraForStart += orientationHelper.StartAfterPadding;
            extraForEnd += orientationHelper.EndPadding;

            if (state.IsPreLayout && pendingScrollPosition != RecyclerView.NoPosition
                                  && pendingScrollPositionOffset != InvalidOffset)
            {
                var existingView = FindViewByPosition(pendingScrollPosition);
                if (existingView != null)
                {
                    var currentOffset = orientationHelper.GetDecoratedStart(existingView) - orientationHelper.StartAfterPadding;
                    int upcomingOffset = pendingScrollPositionOffset - currentOffset;

                    if (upcomingOffset > 0)
                    {
                        extraForStart += upcomingOffset;
                    }
                    else
                    {
                        extraForEnd -= upcomingOffset;
                    }

                }
            }

            int startOffset;
            int endOffset;

            anchors.Clear();
            anchoredViews.Clear();
            anchoredViewsPositions.Clear();
            DetachAndScrapAttachedViews(recycler);

            layoutState.IsPreLayout = state.IsPreLayout;

            if (anchorInfo.LayoutFillingStartsFromEnd)
            {
                //fill towards the start
                updateLayoutStateToFillStart();
                layoutState.Extra = extraForStart;
                fill(recycler, state);
                startOffset = layoutState.Offset;
                var firstElement = layoutState.CurrentAnchorPosition;
                if (layoutState.Available > 0)
                {
                    extraForEnd += layoutState.Available;
                }

                //fill towards the end
                updateLayoutStateToFillEnd();
                layoutState.Extra = extraForEnd;
                layoutState.CurrentAnchorPosition += layoutState.ItemDirection;
                fill(recycler, state);
                endOffset = layoutState.Offset;

                if (layoutState.Available > 0)
                {
                    //end could not consume all space
                    extraForStart = layoutState.Available;
                    updateLayoutStateToFillStart(firstElement, startOffset);
                    layoutState.Extra = extraForStart;
                }
            }
            else
            {
                //fill towards the end
                updateLayoutStateToFillEnd();
                layoutState.Extra = extraForEnd;
                fill(recycler, state);
                endOffset = layoutState.Offset;
                var lastElement = layoutState.CurrentAnchorPosition;
                if (layoutState.Available > 0)
                {
                    extraForStart += layoutState.Available;
                }

                //fill towards the start
                updateLayoutStateToFillStart();
                layoutState.Extra = extraForStart;
                layoutState.CurrentAnchorPosition += layoutState.ItemDirection;
                fill(recycler, state);
                startOffset = layoutState.Offset;

                if (layoutState.Available > 0)
                {
                    extraForEnd = layoutState.Available;
                    updateLayoutStateToFillEnd(lastElement, endOffset);
                    layoutState.Extra = extraForEnd;
                    fill(recycler, state);
                    endOffset = layoutState.Offset;
                }
            }

            if (ChildCount > 0)
            {
                int fixOffset = fixLayoutStartGap(startOffset, recycler, state);
                endOffset += fixOffset;
                fixLayoutEndGap(endOffset + fixOffset, recycler, state);
            }

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

        public override void ScrollToPosition(int position)
        {
            pendingScrollPosition = position;
            pendingScrollPositionOffset = InvalidScrollingOffset;
            pendingAnchorSavedState?.InvalidateAnchor();
            RequestLayout();
        }

        public override void SmoothScrollToPosition(RecyclerView recyclerView, RecyclerView.State state, int position)
        {
            var linearSmoothScroller = new LinearSmoothScroller(recyclerView.Context);
            linearSmoothScroller.TargetPosition = position;
            StartSmoothScroll(linearSmoothScroller);
            base.SmoothScrollToPosition(recyclerView, state, position);
        }

        public override IParcelable OnSaveInstanceState()
        {
            var superState = base.OnSaveInstanceState() ?? new Bundle();
            if (pendingAnchorSavedState != null)
            {
                return new AnchorSavedState(pendingAnchorSavedState, superState);
            }

            var saveState = new AnchorSavedState(superState);
            if (ChildCount > 0)
            {
                var refAnchor = getChildClosestToStart();
                saveState.AnchorShouldLayoutFromEnd = false;
                saveState.TopAnchorPosition = GetPosition(refAnchor);
                saveState.AnchorOffset = orientationHelper.GetDecoratedStart(refAnchor) - orientationHelper.StartAfterPadding;
            }
            else
            {
                saveState.InvalidateAnchor();
            }

            return saveState;
        }

        public override void OnRestoreInstanceState(IParcelable state)
        {
            base.OnRestoreInstanceState(state);
            if (!(state is AnchorSavedState savedState))
            {
                base.OnRestoreInstanceState(state);
                return;
            }

            base.OnRestoreInstanceState(savedState.SuperState);
            pendingAnchorSavedState = savedState;
            RequestLayout();
        }

        private int fixLayoutStartGap(int startOffset, RecyclerView.Recycler recycler, RecyclerView.State state)
        {
            var gap = startOffset - orientationHelper.StartAfterPadding;
            if (gap <= 0)
            {
                // nothing to fix
                return 0;
            }

            var fixOffset = -scrollBy(gap, recycler, state);
            startOffset += fixOffset;
            gap = startOffset - orientationHelper.StartAfterPadding;

            if (gap > 0)
            {
                orientationHelper.OffsetChildren(-gap);
                return fixOffset - gap;
            }

            return fixOffset;
        }

        private int fixLayoutEndGap(int endOffset, RecyclerView.Recycler recycler, RecyclerView.State state)
        {
            var gap = orientationHelper.EndAfterPadding - endOffset;
            if (gap <= 0)
                return 0;

            return -scrollBy(-gap, recycler, state);
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
            if (layoutState.ScrollingOffset != InvalidScrollingOffset)
            {
                if (layoutState.Available < 0)
                {
                    layoutState.ScrollingOffset += layoutState.Available;
                }

                recycleByLayoutState(recycler);
            }

            var remainingSpace = layoutState.Available + layoutState.Extra;

            while (remainingSpace > 0 && layoutState.HasMoreAnchorsToLayout())
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
                    remainingSpace -= layoutChunkResult.Consumed;
                }

                if (layoutState.ScrollingOffset != InvalidScrollingOffset)
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

#if DEBUG
            showLayout();
#endif

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

            if (!(view.Tag is Anchor anchor)) return;

            //todo: check for scrap list if we do predictive animations
            if (layoutState.LayoutDirection == TowardsTheEnd)
                addAnchor(view);
            else
                addAnchor(view, 0);

            MeasureChildWithMargins(view, 0, anchor.Height);
            layoutChunkResult.Consumed = anchor.Height;

            var anchorLeft = PaddingLeft;
            var anchorRight = anchorLeft + view.MeasuredWidth;

            int anchorTop;
            int anchorBottom;

            if (layoutState.LayoutDirection == TowardsTheStart)
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
                if (anchoredViewsPositions.Get(anchorData.AdapterPosition) != null) continue;

                var anchoredView = recycler.GetViewForPosition(anchorData.AdapterPosition);
                var anchoredViewLeft = anchorLeft + anchorData.LeftOffset;
                var anchoredViewTop = anchorTop + anchorData.TopOffset;

                if (layoutState.LayoutDirection == TowardsTheEnd)
                {
                    addAnchoredView(anchoredView, anchorData.AdapterPosition);
                }
                else
                {
                    addAnchoredView(anchoredView, anchorData.AdapterPosition, 0);
                }

                MeasureChildWithMargins(anchoredView, anchorData.Width, anchorData.Height);
                LayoutDecoratedWithMargins(anchoredView,
                    anchoredViewLeft,
                    anchoredViewTop,
                    anchoredViewLeft + anchorData.Width,
                    anchoredViewTop + anchorData.Height);
            }
        }

        private void addAnchor(View child, int index = -1)
        {
            if (index < 0)
            {
                anchors.Add(child);
                AddView(child);
                return;
            }

            anchors.Insert(index, child);
            AddView(child, index);
        }

        private void addAnchoredView(View child, int adapterPosition, int index = -1)
        {
            anchoredViewsPositions.Put(adapterPosition, child);
            if (index < 0)
            {
                anchoredViews.Add(child);
                AddView(child);
                return;
            }

            anchoredViews.Insert(index, child);
            AddView(child, index);
        }

        private void updateAnchorInfoForLayout(RecyclerView.State state)
        {
            if (tryUpdateAnchorInfoFromPendingData(state)) return;

            if (tryUpdateAnchorInfoFromChildren()) return;

            anchorInfo.AssignCoordinateFromPadding();
            anchorInfo.Position = 0;
        }

        private bool tryUpdateAnchorInfoFromPendingData(RecyclerView.State state)
        {
            if (state.IsPreLayout || pendingScrollPosition == RecyclerView.NoPosition)
            {
                return false;
            }

            if (pendingScrollPosition < 0 || pendingScrollPosition >= state.ItemCount)
            {
                pendingScrollPosition = RecyclerView.NoPosition;
                pendingScrollPositionOffset = InvalidOffset;
                //invalid scroll position
                return false;
            }

            anchorInfo.Position = pendingScrollPosition;

            if (pendingAnchorSavedState != null && pendingAnchorSavedState.HasValidAnchor())
            {
                anchorInfo.LayoutFillingStartsFromEnd = pendingAnchorSavedState.AnchorShouldLayoutFromEnd;
                if (anchorInfo.LayoutFillingStartsFromEnd)
                {
                    anchorInfo.Coordinate = orientationHelper.EndAfterPadding - pendingAnchorSavedState.AnchorOffset;
                }
                else
                {
                    anchorInfo.Coordinate = orientationHelper.StartAfterPadding + pendingAnchorSavedState.AnchorOffset;
                }

                return true;
            }

            if (pendingScrollPositionOffset == InvalidOffset)
            {
                var child = FindViewByPosition(pendingScrollPosition);

                if (child != null)
                {
                    var startGap = orientationHelper.GetDecoratedStart(child) - orientationHelper.StartAfterPadding;
                    if (startGap < 0)
                    {
                        anchorInfo.Coordinate = orientationHelper.StartAfterPadding;
                        anchorInfo.LayoutFillingStartsFromEnd = false;
                        return true;
                    }

                    var endGap = orientationHelper.EndAfterPadding - orientationHelper.GetDecoratedEnd(child);
                    if (endGap < 0)
                    {
                        anchorInfo.Coordinate = orientationHelper.EndAfterPadding;
                        anchorInfo.LayoutFillingStartsFromEnd = true;
                        return true;
                    }

                    anchorInfo.Coordinate = anchorInfo.LayoutFillingStartsFromEnd
                        ? orientationHelper.GetDecoratedEnd(child) + orientationHelper.TotalSpaceChange
                        : orientationHelper.GetDecoratedStart(child);
                }
                else
                { // anchor is not visible
                    if (ChildCount > 0)
                    {
                        var anchorPosition = GetPosition(GetChildAt(0));
                        anchorInfo.LayoutFillingStartsFromEnd = pendingScrollPosition >= anchorPosition;
                    }
                    anchorInfo.AssignCoordinateFromPadding();
                }

                return true;
            }

            anchorInfo.LayoutFillingStartsFromEnd = false;
            anchorInfo.Coordinate = orientationHelper.StartAfterPadding + pendingScrollPosition;

            return true;
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

        private void updateLayoutStateToFillStart()
        {
            updateLayoutStateToFillStart(anchorInfo.Position, anchorInfo.Coordinate);
        }

        private void updateLayoutStateToFillEnd()
        {
            updateLayoutStateToFillEnd(anchorInfo.Position, anchorInfo.Coordinate);
        }

        private void updateLayoutStateToFillStart(int anchorPosition, int offset)
        {
            layoutState.Offset = offset;
            layoutState.Available = offset - orientationHelper.StartAfterPadding;
            layoutState.CurrentAnchorPosition = anchorPosition;
            layoutState.ItemDirection = TowardsTheStart;
            layoutState.LayoutDirection = TowardsTheStart;
            layoutState.ScrollingOffset = InvalidScrollingOffset;
        }

        private void updateLayoutStateToFillEnd(int anchorPosition, int offset)
        {
            layoutState.Offset = offset;
            layoutState.Available = orientationHelper.EndAfterPadding - offset;
            layoutState.CurrentAnchorPosition = anchorPosition;
            layoutState.ItemDirection = TowardsTheEnd;
            layoutState.LayoutDirection = TowardsTheEnd;
            layoutState.ScrollingOffset = InvalidScrollingOffset;
        }

        private int scrollBy(int dy, RecyclerView.Recycler recycler, RecyclerView.State state)
        {
            if (ChildCount == 0 || dy == 0)
                return 0;

            layoutState.Recycle = true;

            var layoutDirection = dy > 0 ? TowardsTheEnd : TowardsTheStart;
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
            if (layoutDirection == TowardsTheEnd)
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
