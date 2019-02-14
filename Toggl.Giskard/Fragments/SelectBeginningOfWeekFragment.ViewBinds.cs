using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Views;

namespace Toggl.Giskard.Fragments
{
    public partial class SelectBeginningOfWeekFragment
    {
        private RecyclerView recyclerView;
        
        private void initializeViews(View view)
        {
            recyclerView = view.FindViewById<RecyclerView>(Resource.Id.DaysListRecyclerView);
        }
    }
}
