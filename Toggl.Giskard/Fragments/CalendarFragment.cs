using Android.OS;
using Android.Views;
using Toggl.Foundation.MvvmCross.ViewModels.Calendar;
using Toggl.Giskard.Adapters.Calendar;
using Toggl.Giskard.Views.Calendar;

namespace Toggl.Giskard.Fragments
{
    public partial class CalendarFragment : ReactiveFragment<CalendarViewModel>
    {
        private CalendarLayoutManager calendarLayoutManager;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.CalendarFragment, container, false);
            InitializeViews(view);

            calendarLayoutManager = new CalendarLayoutManager();
            calendarRecyclerView.SetLayoutManager(calendarLayoutManager);
            calendarRecyclerView.SetAdapter(new CalendarAdapter(view.Context));

            return view;
        }
    }
}
