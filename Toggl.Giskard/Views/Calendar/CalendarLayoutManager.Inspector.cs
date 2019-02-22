using Android.Support.V7.Widget;
using Android.Util;

namespace Toggl.Giskard.Views.Calendar
{
    public partial class CalendarLayoutManager
    {
#if DEBUG
        private const string CALENDAR_TAG = "CALENDAR_TAG";

        private void showLayout()
        {
            Log.Info(CALENDAR_TAG, "layout: begin");
            for (var i = 0; i < ChildCount; i++)
            {
                printChild(i);
            }

            Log.Info(CALENDAR_TAG, "layout: end");
        }

        private void printChild(int childIndex)
        {
            var child = GetChildAt(childIndex);
            if (child == null) return;
            var lp = (RecyclerView.LayoutParams) child.LayoutParameters;
            Log.Info(CALENDAR_TAG, $"layout c: {childIndex:00} lm: {lp.ViewLayoutPosition:00} a: {lp.ViewAdapterPosition:00} {(isAnchor(child) ? 'a' : 'x')}");
        }

#endif
    }
}
