using System.Collections.Generic;
using Android.Support.V7.Widget;
using Android.Views;

namespace Toggl.Giskard.Views.Calendar
{
    public partial class CalendarLayoutManager
    {
        private const int TowardsTheEnd = 1;
        private const int TowardsTheStart = -1;
        private const int InvalidScrollingOffset = int.MinValue;

        private struct LayoutState
        {
            private int anchorCount;

            public int Offset { get; set; }

            public int ScrollingOffset { get; set; }

            public int Available { get; set; }

            public int Extra { get; set; }

            public int CurrentAnchorPosition { get; set; }

            public int LastScrollDelta { get; set; }

            public int ItemDirection { get; set; }

            public int LayoutDirection { get; set; }

            public bool IsPreLayout { get; set; }

            public bool Recycle { get; set; }

            public IList<RecyclerView.ViewHolder> ScrapList { get; set; }

            public LayoutState(int anchorCount)
            {
                this.anchorCount = anchorCount;
                Offset = 0;
                ScrollingOffset = 0;
                Available = 0;
                CurrentAnchorPosition = 0;
                LastScrollDelta = 0;
                Extra = 0;
                ItemDirection = TowardsTheEnd;
                LayoutDirection = TowardsTheEnd;
                Recycle = false;
                IsPreLayout = false;
                ScrapList = null;
            }

            public bool HasMoreAnchorsToLayout()
                => CurrentAnchorPosition >= 0 && CurrentAnchorPosition < anchorCount;

            public View Next(RecyclerView.Recycler recycler)
            {
                if (ScrapList != null)
                {
                    //todo: check in the scrapList when laying out for predictive animations (if we do it)
                }

                var view = recycler.GetViewForPosition(CurrentAnchorPosition);
                CurrentAnchorPosition += ItemDirection;
                return view;
            }
        }
    }
}
