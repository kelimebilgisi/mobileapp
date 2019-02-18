using Android.Support.V7.Widget;

namespace Toggl.Giskard.Views.Calendar
{
    public class CalendarLayoutManager : RecyclerView.LayoutManager
    {
        public override RecyclerView.LayoutParams GenerateDefaultLayoutParams()
        {
            return new RecyclerView.LayoutParams(RecyclerView.LayoutParams.WrapContent,RecyclerView.LayoutParams.WrapContent);
        }

        public override bool CanScrollVertically() => true;

        public override bool CanScrollHorizontally() => false;

        public override void OnLayoutChildren(RecyclerView.Recycler recycler, RecyclerView.State state)
        {
            //todo: layout children;
            //fill screen with views;
            //init layout state
            //restore state;
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
    }
}
