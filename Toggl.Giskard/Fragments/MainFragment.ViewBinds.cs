using Android.Support.Design.Widget;
using Android.Support.V4.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Giskard.Fragments
{
    public sealed partial class MainFragment
    {
        private View runningEntryCardFrame;
        private FloatingActionButton playButton;
        private FloatingActionButton stopButton;
        private CoordinatorLayout coordinatorLayout;
        private View timeEntryCard;
        private TextView timeEntryCardTimerLabel;
        private TextView timeEntryCardDescriptionLabel;
        private TextView timeEntryCardAddDescriptionLabel;
        private View timeEntryCardDotContainer;
        private View timeEntryCardDotView;
        private TextView timeEntryCardProjectClientTaskLabel;
        private SwipeRefreshLayout refreshLayout;
        private RecyclerView mainRecyclerView;
        private ViewStub emptyStateViewStub;
        private View emptyStateView;
        private ViewStub welcomeBackStub;
        private View welcomeBackView;
        private Toolbar toolbar;

        protected override void InitializeViews(View fragmentView)
        {
            mainRecyclerView = fragmentView.FindViewById<RecyclerView>(Resource.Id.MainRecyclerView);
            runningEntryCardFrame = fragmentView.FindViewById(Resource.Id.MainRunningTimeEntryFrame);
            playButton = fragmentView.FindViewById<FloatingActionButton>(Resource.Id.MainPlayButton);
            stopButton = fragmentView.FindViewById<FloatingActionButton>(Resource.Id.MainStopButton);
            coordinatorLayout = fragmentView.FindViewById<CoordinatorLayout>(Resource.Id.MainCoordinatorLayout);
            timeEntryCard = fragmentView.FindViewById(Resource.Id.MainContentArea);
            timeEntryCardTimerLabel = fragmentView.FindViewById<TextView>(Resource.Id.MainRunningTimeEntryTimerLabel);
            timeEntryCardDescriptionLabel = fragmentView.FindViewById<TextView>(Resource.Id.MainRunningTimeEntryDescription);
            timeEntryCardAddDescriptionLabel = fragmentView.FindViewById<TextView>(Resource.Id.MainRunningTimeEntryAddDescriptionLabel);
            timeEntryCardDotContainer = fragmentView.FindViewById(Resource.Id.MainRunningTimeEntryProjectDotContainer);
            timeEntryCardDotView = fragmentView.FindViewById(Resource.Id.MainRunningTimeEntryProjectDotView);
            timeEntryCardProjectClientTaskLabel = fragmentView.FindViewById<TextView>(Resource.Id.MainRunningTimeEntryProjectClientTaskLabel);
            refreshLayout = fragmentView.FindViewById<SwipeRefreshLayout>(Resource.Id.MainSwipeRefreshLayout);
            emptyStateViewStub = fragmentView.FindViewById<ViewStub>(Resource.Id.EmptyStateViewStub);
            welcomeBackStub = fragmentView.FindViewById<ViewStub>(Resource.Id.WelcomeBackViewStub);
            toolbar = fragmentView.FindViewById<Toolbar>(Resource.Id.Toolbar);
        }
    }
}
