using System;
using Android.Support.V7.Widget;
using Android.Views;

namespace Toggl.Giskard.Views.Calendar
{
    public partial class CalendarLayoutManager
    {
        private int computeScrollOffset(RecyclerView.State state, View startChild, View endChild)
        {
            if (cannotCalculateScrollVariables(state, startChild, endChild))
                return 0;

            var startChildPosition = GetPosition(startChild);
            var endChildPosition = GetPosition(endChild);
            var minPosition = Math.Min(startChildPosition, endChildPosition);

            var itemsBeforeStart = Math.Max(0, minPosition - 1);

            var laidOutArea = Math.Abs(orientationHelper.GetDecoratedEnd(endChild) - orientationHelper.GetDecoratedStart(startChild));
            var itemRange = Math.Abs(startChildPosition - endChildPosition) + 1;
            var avgSizePerRow = (float)laidOutArea / itemRange;
            //todo: simplify to use anchor height
            return (int)Math.Round(itemsBeforeStart * avgSizePerRow +
                                    (orientationHelper.StartAfterPadding - orientationHelper.GetDecoratedStart(startChild)));
        }

        private int computeScrollExtent(RecyclerView.State state, View startChild, View endChild)
        {
            if (cannotCalculateScrollVariables(state, startChild, endChild))
                return 0;

            var extent = orientationHelper.GetDecoratedEnd(endChild) - orientationHelper.GetDecoratedStart(startChild);
            return Math.Min(orientationHelper.TotalSpace, extent);
        }

        private int computeScrollRange(RecyclerView.State state, View startChild, View endChild)
        {
            if (cannotCalculateScrollVariables(state, startChild, endChild))
                return 0;

            var laidOutArea = orientationHelper.GetDecoratedEnd(endChild) - orientationHelper.GetDecoratedStart(startChild);
            var laidOutRangeItemCount = Math.Abs(GetPosition(startChild) - GetPosition(endChild)) + 1;

            //todo: simplify to use only the anchor height * anchor count
            return (int)((float)laidOutArea / laidOutRangeItemCount * anchorCount);
        }

        private bool cannotCalculateScrollVariables(RecyclerView.State state, View startChild, View endChild)
            => ChildCount == 0 || state.ItemCount == 0 || startChild == null || endChild == null;
    }
}
