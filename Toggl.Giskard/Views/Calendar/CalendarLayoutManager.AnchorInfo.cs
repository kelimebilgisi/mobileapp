using Android.Support.V7.Widget;
using Android.Views;

namespace Toggl.Giskard.Views.Calendar
{
    public partial class CalendarLayoutManager
    {
        private const int InvalidOffset = int.MinValue;

        private struct AnchorInfo
        {
            private OrientationHelper OrientationHelper { get; set; }

            public int Position { get; set; }

            public int Coordinate { get; set; }

            public bool IsValid { get; set; }

            public bool LayoutFillingStartsFromEnd { get; set; }

            private int anchorCount;

            public AnchorInfo(OrientationHelper orientationHelper, int anchorCount)
            {
                this.anchorCount = anchorCount;
                OrientationHelper = orientationHelper;
                Position = RecyclerView.NoPosition;
                Coordinate = InvalidOffset;
                IsValid = false;
                LayoutFillingStartsFromEnd = false;
            }

            public void Reset()
            {
                Position = RecyclerView.NoPosition;
                Coordinate = InvalidOffset;
                IsValid = false;
                LayoutFillingStartsFromEnd = false;
            }

            public void AssignCoordinateFromPadding()
            {
                Coordinate = LayoutFillingStartsFromEnd
                    ? OrientationHelper.EndAfterPadding
                    : OrientationHelper.StartAfterPadding;
            }

            public void AssignFromView(View referenceChild, int position)
            {
                Coordinate = LayoutFillingStartsFromEnd
                    ? OrientationHelper.GetDecoratedEnd(referenceChild) + OrientationHelper.TotalSpaceChange
                    : OrientationHelper.GetDecoratedStart(referenceChild);

                Position = position;
            }
        }
    }
}
