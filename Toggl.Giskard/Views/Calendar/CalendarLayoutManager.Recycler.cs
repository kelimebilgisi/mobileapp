using System;
using Android.Support.V7.Widget;

namespace Toggl.Giskard.Views.Calendar
{
    public partial class CalendarLayoutManager
    {
        private void recycleByLayoutState(RecyclerView.Recycler recycler)
        {
            if (!layoutState.Recycle || layoutState.ScrollingOffset < 0) return;

            if (layoutState.LayoutDirection == TowardsTheStart)
            {
                recycleAnchorsFromEnd(recycler);
                recycleAnchoredViewsFromEnd(recycler);
            }
            else
            {
                recycleAnchorsFromStart(recycler);
                recycleAnchoredViewsFromStart(recycler);
            }
        }

        private void recycleAnchorsFromEnd(RecyclerView.Recycler recycler)
        {
            recycleViewsFromEnd(recycler, true);
        }

        private void recycleAnchoredViewsFromEnd(RecyclerView.Recycler recycler)
        {
            recycleViewsFromEnd(recycler, false);
        }

        private void recycleAnchorsFromStart(RecyclerView.Recycler recycler)
        {
            recycleViewsFromStart(recycler, true);
        }

        private void recycleAnchoredViewsFromStart(RecyclerView.Recycler recycler)
        {
            recycleViewsFromStart(recycler, false);
        }

        private void recycleViewsFromEnd(RecyclerView.Recycler recycler, bool isRecyclingAnchors)
        {
            var limit = orientationHelper.End - layoutState.ScrollingOffset;
            var source = isRecyclingAnchors ? anchors : anchoredViews;
            var laidOutViewsCount = source.Count;
            for (var i = laidOutViewsCount - 1; i >= 0; i--)
            {
                var child = source[i];
                if (orientationHelper.GetDecoratedStart(child) < limit || orientationHelper.GetTransformedStartWithDecoration(child) < limit)
                {
                    recycleChildren(recycler, laidOutViewsCount - 1, i, isRecyclingAnchors);
                    return;
                }
            }
        }

        private void recycleViewsFromStart(RecyclerView.Recycler recycler, bool isRecyclingAnchors)
        {
            var limit = layoutState.ScrollingOffset;
            var source = isRecyclingAnchors ? anchors : anchoredViews;
            var laidOutViewsCount = source.Count;
            for (var i = 0; i < laidOutViewsCount; i++)
            {
                var child = source[i];
                if (orientationHelper.GetDecoratedEnd(child) > limit || orientationHelper.GetTransformedEndWithDecoration(child) > limit)
                {
                    recycleChildren(recycler, 0, i, isRecyclingAnchors);
                    return;
                }
            }
        }

        private void recycleChildren(RecyclerView.Recycler recycler, int startIndex, int endIndex, bool isRecyclingAnchors)
        {
            if (startIndex == endIndex) return;

            recycleAndUpdateBookkeeping(recycler, startIndex, endIndex, isRecyclingAnchors);
        }

        private void recycleAndUpdateBookkeeping(RecyclerView.Recycler recycler, int startIndex, int endIndex, bool isRecyclingAnchors)
        {
            var source = isRecyclingAnchors ? anchors : anchoredViews;
            if (isRecyclingAnchors)
                recycle(recycler, startIndex, endIndex, removeAndRecycleAnchor);
            else
                recycle(recycler, startIndex, endIndex, removeAndRecycleAnchoredView);

            source.RemoveAll(v => v == null);
        }

        private void recycle(RecyclerView.Recycler recycler, int startIndex, int endIndex, Action<int, RecyclerView.Recycler> recyclingMethod)
        {
            if (endIndex > startIndex)
            {
                for (var i = endIndex - 1; i >= startIndex; i--)
                {
                    recyclingMethod(i, recycler);
                }
            }
            else
            {
                for (var i = startIndex; i > endIndex; i--)
                {
                    recyclingMethod(i, recycler);
                }
            }
        }

        private void removeAndRecycleAnchoredView(int position, RecyclerView.Recycler recycler)
        {
            var adapterPosition = ((RecyclerView.LayoutParams) anchoredViews[position].LayoutParameters).ViewAdapterPosition;
            RemoveAndRecycleView(anchoredViews[position], recycler);
            anchoredViews[position] = null;
            anchoredViewsPositions.Remove(adapterPosition);
        }

        private void removeAndRecycleAnchor(int position, RecyclerView.Recycler recycler)
        {
            RemoveAndRecycleView(anchors[position], recycler);
            anchors[position] = null;
        }
    }
}
