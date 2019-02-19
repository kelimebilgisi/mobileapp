using System.Collections.Generic;
using Android.Support.V7.Widget;

namespace Toggl.Giskard.Views.Calendar
{
    public partial class CalendarLayoutManager
    {
        private const int TOWARDS_THE_END = 1;
        private const int TOWARDS_THE_START = -1;
        private const int INVALID_SCROLLING_OFFSET = int.MinValue;

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
                ItemDirection = TOWARDS_THE_END;
                LayoutDirection = TOWARDS_THE_END;
                Recycle = false;
                IsPreLayout = false;
                ScrapList = null;
            }
        }
    }
}
