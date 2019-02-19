using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Foundation.Helper;

namespace Toggl.Giskard.Views.Calendar
{
    public partial class CalendarLayoutManager : RecyclerView.LayoutManager
    {
        private const int anchorCount = Constants.HoursPerDay;
        private OrientationHelper orientationHelper;
        private AnchorInfo anchorInfo;
        private LayoutState layoutState;

        public CalendarLayoutManager()
        {
            orientationHelper = OrientationHelper.CreateVerticalHelper(this);
            anchorInfo = new AnchorInfo(orientationHelper, anchorCount);
            layoutState = new LayoutState(anchorCount);
        }

        public override RecyclerView.LayoutParams GenerateDefaultLayoutParams()
        {
            return new RecyclerView.LayoutParams(RecyclerView.LayoutParams.WrapContent,RecyclerView.LayoutParams.WrapContent);
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

            //todo: calculate extras
            //todo: update layout state to fill layout
            //todo: fill layout
        }

        public override void OnLayoutCompleted(RecyclerView.State state)
        {
            base.OnLayoutCompleted(state);
            //todo: reset internal layout state data
        }

        // don't scroll horizontally
        public override int ScrollHorizontallyBy(int dx, RecyclerView.Recycler recycler, RecyclerView.State state)
            => 0;

        public override int ScrollVerticallyBy(int dy, RecyclerView.Recycler recycler, RecyclerView.State state)
        {
            //todo: handle vertical scroll
            //offset views;
            //handle recycling;
            //fill space created by gaps caused by scroll
            //anchors will be laid out first, followed by anchored views
            return 0;
        }

        public override int ComputeVerticalScrollOffset(RecyclerView.State state)
        {
            //todo: calculate vertical scroll offset based on anchors
            return base.ComputeVerticalScrollOffset(state);
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
                    } else if (matchOutOfBounds == null && isOutOfBounds(candidate))
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
    }
}
