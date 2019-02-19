using Android.Support.V7.Widget;
using Android.Views;

namespace Toggl.Giskard.Views.Calendar
{
    public partial class CalendarLayoutManager
    {
        private const int INVALID_OFFSET = int.MinValue;

        private struct AnchorInfo
        {
            public OrientationHelper OrientationHelper { get; private set; }

            public int Position { get; set; }

            public int Coordinate { get; set; }

            public bool IsValid { get; set; }

            private int anchorCount;

            public AnchorInfo(OrientationHelper orientationHelper, int anchorCount)
            {
                this.anchorCount = anchorCount;
                OrientationHelper = orientationHelper;
                Position = RecyclerView.NoPosition;
                Coordinate = INVALID_OFFSET;
                IsValid = false;
            }

            public void Reset()
            {
                Position = RecyclerView.NoPosition;
                Coordinate = INVALID_OFFSET;
                IsValid = false;
            }

            public bool IsViewValidAsAnchor(View view)
            {
                if (!(view.LayoutParameters is RecyclerView.LayoutParams layoutParams))
                    return false;

                return !layoutParams.IsItemRemoved
                       && layoutParams.ViewLayoutPosition >= 0
                       && layoutParams.ViewLayoutPosition < anchorCount;
            }
        }
    }
}
