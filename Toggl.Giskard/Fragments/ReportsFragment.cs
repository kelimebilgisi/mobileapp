using System;
using System.Reactive.Linq;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels.Reports;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Giskard.ViewHelpers;
using Toggl.Multivac.Extensions;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Giskard.Fragments
{
    public sealed partial class ReportsFragment : ReactiveFragment<ReportsViewModel>
    {
        private static readonly TimeSpan toggleCalendarThrottleDuration = TimeSpan.FromMilliseconds(300);
        private ReportsRecyclerAdapter reportsRecyclerAdapter;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.ReportsFragment, container, false);
            InitializeViews(view);
            setupToolbar();

            selectWorkspaceFAB.Rx().Tap()
                .Subscribe(ViewModel.SelectWorkspace.Inputs)
                .DisposedBy(DisposeBag);

            calendarView.SetupWith(ViewModel.CalendarViewModel);

            setupReportsRecyclerView();
            ViewModel.StartDate.CombineLatest(
                    ViewModel.EndDate,
                    ViewModel.WorkspaceHasBillableFeatureEnabled,
                    ViewModel.BarChartViewModel.DateFormat,
                    ViewModel.BarChartViewModel.Bars,
                    ViewModel.BarChartViewModel.MaximumHoursPerBar,
                    ViewModel.BarChartViewModel.HorizontalLegend,
                    BarChartData.Create)
                .Subscribe(reportsRecyclerAdapter.UpdateBarChart)
                .DisposedBy(DisposeBag);

            ViewModel.WorkspaceNameObservable
                .Subscribe(reportsRecyclerAdapter.UpdateWorkspaceName)
                .DisposedBy(DisposeBag);

            ViewModel.SegmentsObservable.CombineLatest(
                    ViewModel.ShowEmptyStateObservable,
                    ViewModel.TotalTimeObservable,
                    ViewModel.TotalTimeIsZeroObservable,
                    ViewModel.BillablePercentageObservable,
                    ViewModel.DurationFormatObservable,
                    ReportsSummaryData.Create)
                .Subscribe(reportsRecyclerAdapter.UpdateReportsSummary)
                .DisposedBy(DisposeBag);

            reportsRecyclerAdapter.SummaryCardClicks
                .Subscribe(ViewModel.HideCalendar)
                .DisposedBy(DisposeBag);

            toolbarCurrentDateRangeText.Rx().Tap()
                .Throttle(toggleCalendarThrottleDuration)
                .Subscribe(ViewModel.ToggleCalendar)
                .DisposedBy(DisposeBag);

            ViewModel.CurrentDateRangeStringObservable
                .Subscribe(toolbarCurrentDateRangeText.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            return view;
        }

        private void setupReportsRecyclerView()
        {
            reportsRecyclerAdapter = new ReportsRecyclerAdapter(Context);
            reportsRecyclerView.SetLayoutManager(new LinearLayoutManager(Context));
            reportsRecyclerView.SetAdapter(reportsRecyclerAdapter);
        }

        internal void ToggleCalendarState(bool forceHide)
        {
            reportsMainContainer.ToggleCalendar(forceHide);
        }

        private void setupToolbar()
        {
            var activity = Activity as AppCompatActivity;
            toolbar.Title = "";
            activity.SetSupportActionBar(toolbar);
        }
    }
}
