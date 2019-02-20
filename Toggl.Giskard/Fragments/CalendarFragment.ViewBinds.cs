using Android.Views;
using Toggl.Giskard.Views;

namespace Toggl.Giskard.Fragments
{
    public partial class CalendarFragment
    {
        private CalendarRecyclerView calendarRecyclerView;

        protected override void InitializeViews(View view)
        {
            calendarRecyclerView = view.FindViewById<CalendarRecyclerView>(Resource.Id.calendarRecyclerView);
        }
    }
}
