using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.Content;
using Android.Support.V7.Widget;
using Android.Support.V7.Widget.Helper;
using Android.Text;
using Android.Views;
using MvvmCross.Platforms.Android.Core;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.TimeEntriesLog;
using Toggl.Foundation.Sync;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Giskard.Helper;
using Toggl.Giskard.Services;
using Toggl.Giskard.ViewHelpers;
using Toggl.Multivac.Extensions;
using static Toggl.Foundation.Sync.SyncProgress;
using static Toggl.Giskard.Extensions.CircularRevealAnimation.AnimationType;
using FoundationResources = Toggl.Foundation.Resources;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme",
              ScreenOrientation = ScreenOrientation.Portrait,
              ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public sealed partial class MainActivity : ReactiveActivity<MainViewModel>
    {
        private const int snackbarDuration = 5000;
        private NotificationManager notificationManager;
        private MainRecyclerAdapter mainRecyclerAdapter;
        private LinearLayoutManager layoutManager;
        private FirebaseStopwatchProviderAndroid localStopwatchProvider = new FirebaseStopwatchProviderAndroid();
        private CancellationTokenSource cardAnimationCancellation;

        protected override void OnCreate(Bundle bundle)
        {
            var setup = MvxAndroidSetupSingleton.EnsureSingletonAvailable(ApplicationContext);
            setup.EnsureInitialized();

            base.OnCreate(bundle);
            var onCreateStopwatch = localStopwatchProvider.Create(MeasuredOperation.MainActivityOnCreate);
            onCreateStopwatch.Start();
            SetContentView(Resource.Layout.MainActivity);
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_fade_out);

            InitializeViews();

            SetSupportActionBar(FindViewById<Toolbar>(Resource.Id.Toolbar));
            SupportActionBar.SetDisplayShowHomeEnabled(false);
            SupportActionBar.SetDisplayShowTitleEnabled(false);

            runningEntryCardFrame.Visibility = ViewStates.Invisible;

            reportsView.Rx().BindAction(ViewModel.OpenReports).DisposedBy(DisposeBag);
            settingsView.Rx().BindAction(ViewModel.OpenSettings).DisposedBy(DisposeBag);
            stopButton.Rx().BindAction(ViewModel.StopTimeEntry, _ => TimeEntryStopOrigin.Manual).DisposedBy(DisposeBag);

            playButton.Rx().BindAction(ViewModel.StartTimeEntry, _ => true).DisposedBy(DisposeBag);
            playButton.Rx().BindAction(ViewModel.StartTimeEntry, _ => false, ButtonEventType.LongPress).DisposedBy(DisposeBag);

            timeEntryCard.Rx().Tap()
                .WithLatestFrom(ViewModel.CurrentRunningTimeEntry, (_, te) => te.Id)
                .Subscribe(ViewModel.SelectTimeEntry.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.ElapsedTime
                .Subscribe(timeEntryCardTimerLabel.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.CurrentRunningTimeEntry
                .Select(te => te?.Description ?? "")
                .Subscribe(timeEntryCardDescriptionLabel.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.CurrentRunningTimeEntry
                .Select(te => string.IsNullOrWhiteSpace(te?.Description))
                .Subscribe(timeEntryCardAddDescriptionLabel.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.CurrentRunningTimeEntry
                .Select(te => string.IsNullOrWhiteSpace(te?.Description))
                .Invert()
                .Subscribe(timeEntryCardDescriptionLabel.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.CurrentRunningTimeEntry
                .Select(CreateProjectClientTaskLabel)
                .Subscribe(timeEntryCardProjectClientTaskLabel.Rx().TextFormattedObserver())
                .DisposedBy(DisposeBag);

            var projectVisibilityObservable = ViewModel.CurrentRunningTimeEntry
                .Select(te => te?.Project != null);

            projectVisibilityObservable
                .Subscribe(timeEntryCardProjectClientTaskLabel.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            projectVisibilityObservable
                .Subscribe(timeEntryCardDotContainer.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            var projectColorObservable = ViewModel.CurrentRunningTimeEntry
                .Select(te => te?.Project?.Color ?? "#000000")
                .Select(Color.ParseColor);

            projectColorObservable
                .Subscribe(timeEntryCardDotView.Rx().DrawableColor())
                .DisposedBy(DisposeBag);

            var addDrawable = ContextCompat.GetDrawable(this, Resource.Drawable.add_white);
            var playDrawable = ContextCompat.GetDrawable(this, Resource.Drawable.play_white);

            ViewModel.IsInManualMode
                .Select(isInManualMode => isInManualMode ? addDrawable : playDrawable)
                .Subscribe(playButton.SetImageDrawable)
                .DisposedBy(DisposeBag);

            ViewModel.IsTimeEntryRunning
                .Subscribe(onTimeEntryCardVisibilityChanged)
                .DisposedBy(DisposeBag);

            ViewModel.SyncProgressState
                .Subscribe(onSyncChanged)
                .DisposedBy(DisposeBag);

            mainRecyclerAdapter = new MainRecyclerAdapter(ViewModel.TimeService)
            {
                SuggestionsViewModel = ViewModel.SuggestionsViewModel,
                RatingViewModel = ViewModel.RatingViewModel,
                StopwatchProvider = localStopwatchProvider
            };

            mainRecyclerAdapter.TimeEntryTaps
                .Select(te => te.RepresentedTimeEntriesIds.First())
                .Subscribe(ViewModel.SelectTimeEntry.Inputs)
                .DisposedBy(DisposeBag);

            mainRecyclerAdapter.ContinueTimeEntrySubject
                .Select(vm => vm.RepresentedTimeEntriesIds.First())
                .Subscribe(ViewModel.ContinueTimeEntry.Inputs)
                .DisposedBy(DisposeBag);

            mainRecyclerAdapter.DeleteTimeEntrySubject
                .Select(vm => vm.RepresentedTimeEntriesIds)
                .Subscribe(ViewModel.TimeEntriesViewModel.DelayDeleteTimeEntries.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.TimeEntriesViewModel.TimeEntriesPendingDeletion
                .Subscribe(showUndoDeletion)
                .DisposedBy(DisposeBag);

            ViewModel.SyncProgressState
                .Subscribe(updateSyncingIndicator)
                .DisposedBy(DisposeBag);

           refreshLayout.Rx().Refreshed()
                .Subscribe(ViewModel.Refresh.Inputs)
                .DisposedBy(DisposeBag);

            setupLayoutManager(mainRecyclerAdapter);

            ViewModel.TimeEntries
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(mainRecyclerAdapter.UpdateCollection)
                .DisposedBy(DisposeBag);

            ViewModel.IsTimeEntryRunning
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(updateRecyclerViewPadding)
                .DisposedBy(DisposeBag);

            notificationManager = GetSystemService(NotificationService) as NotificationManager;
            this.BindRunningTimeEntry(notificationManager, ViewModel.CurrentRunningTimeEntry, ViewModel.ShouldShowRunningTimeEntryNotification)
                .DisposedBy(DisposeBag);
            this.BindIdleTimer(notificationManager, ViewModel.IsTimeEntryRunning, ViewModel.ShouldShowStoppedTimeEntryNotification)
                .DisposedBy(DisposeBag);
            setupItemTouchHelper(mainRecyclerAdapter);

            ViewModel.TimeEntriesCount
                .Subscribe(timeEntriesCountSubject)
                .DisposedBy(DisposeBag);

            ViewModel.ShouldReloadTimeEntryLog
                .Subscribe(reload)
                .DisposedBy(DisposeBag);

            ViewModel.ShouldShowWelcomeBack
                .Subscribe(onWelcomeBackViewVisibilityChanged)
                .DisposedBy(DisposeBag);

            ViewModel.ShouldShowEmptyState
                .Subscribe(onEmptyStateVisibilityChanged)
                .DisposedBy(DisposeBag);

            setupOnboardingSteps();
            onCreateStopwatch.Stop();
        }

        public ISpannable CreateProjectClientTaskLabel(IThreadSafeTimeEntry te)
        {
            if (te == null)
                return new SpannableString(string.Empty);

            var hasProject = te.ProjectId != null;
            return Extensions.TimeEntryExtensions.ToProjectTaskClient(hasProject, te.Project?.Name, te.Project?.Color, te.Task?.Name, te.Project?.Client?.Name);
        }

        public void SetupRatingViewVisibility(bool isVisible)
        {
            mainRecyclerAdapter.SetupRatingViewVisibility(isVisible);
        }

        private void reload()
        {
            mainRecyclerAdapter.NotifyDataSetChanged();
        }

        private void setupLayoutManager(MainRecyclerAdapter mainAdapter)
        {
            layoutManager = new LinearLayoutManager(this);
            layoutManager.ItemPrefetchEnabled = true;
            layoutManager.InitialPrefetchItemCount = 4;
            mainRecyclerView.SetLayoutManager(layoutManager);
            mainRecyclerView.SetAdapter(mainAdapter);
        }

        private void setupItemTouchHelper(MainRecyclerAdapter mainAdapter)
        {
            var callback = new MainRecyclerViewTouchCallback(mainAdapter);
            var itemTouchHelper = new ItemTouchHelper(callback);
            itemTouchHelper.AttachToRecyclerView(mainRecyclerView);
        }

        private void updateSyncingIndicator(SyncProgress state)
        {
            refreshLayout.Refreshing = state == Syncing;
        }

        private void updateRecyclerViewPadding(bool isRunning)
        {
            var newPadding = isRunning ? 104.DpToPixels(this) : 70.DpToPixels(this);
            mainRecyclerView.SetPadding(0, 0, 0, newPadding);
        }

        private void onSyncChanged(SyncProgress syncProgress)
        {
            switch (syncProgress)
            {
                case Failed:
                case Unknown:
                case OfflineModeDetected:

                    var errorMessage = syncProgress == OfflineModeDetected
                                     ? FoundationResources.Offline
                                     : FoundationResources.SyncFailed;

                    var snackbar = Snackbar.Make(coordinatorLayout, errorMessage, Snackbar.LengthLong)
                        .SetAction(FoundationResources.TapToRetry, onRetryTapped);
                    snackbar.SetDuration(snackbarDuration);
                    snackbar.Show();
                    break;
            }

            void onRetryTapped(View view)
            {
                ViewModel.Refresh.Execute();
            }
        }

        private void onTimeEntryCardVisibilityChanged(bool visible)
        {
            cardAnimationCancellation?.Cancel();
            if (runningEntryCardFrame == null) return;

            var isCardVisible = runningEntryCardFrame.Visibility == ViewStates.Visible;
            if (isCardVisible == visible) return;

            cardAnimationCancellation = new CancellationTokenSource();

            var buttonToHide = visible ? playButton : stopButton;
            var buttonToShow = visible ? stopButton : playButton;

            var radialAnimation =
                runningEntryCardFrame
                    .AnimateWithCircularReveal()
                    .WithCancellationToken(cardAnimationCancellation.Token)
                    .SetDuration(TimeSpan.FromSeconds(0.5))
                    .SetBehaviour((x, y, w, h) => (x, y + h, 0, w))
                    .SetType(() => visible ? Appear : Disappear);

            var fabListener = new FabVisibilityListener(onFabHidden);
            buttonToHide.Hide(fabListener);

            void onFabHidden()
            {
                radialAnimation
                    .OnAnimationEnd(_ => buttonToShow.Show())
                    .OnAnimationCancel(buttonToHide.Show)
                    .Start();
            }
        }

        private void onEmptyStateVisibilityChanged(bool shouldShowEmptyState)
        {
            if (shouldShowEmptyState)
            {
                if (emptyStateView == null)
                {
                    emptyStateView = emptyStateViewStub.Inflate();
                }

                emptyStateView.Visibility = ViewStates.Visible;
            }
            else if (emptyStateView != null)
            {
                emptyStateView.Visibility = ViewStates.Gone;
            }
        }

        private void showUndoDeletion(int? numberOfTimeEntriesPendingDeletion)
        {
            if (!numberOfTimeEntriesPendingDeletion.HasValue)
                return;

            var undoText = numberOfTimeEntriesPendingDeletion > 1
                ? String.Format(FoundationResources.MultipleEntriesDeleted, numberOfTimeEntriesPendingDeletion)
                : FoundationResources.EntryDeleted;

            Snackbar.Make(coordinatorLayout, undoText, snackbarDuration)
                .SetAction(FoundationResources.UndoButtonTitle, view => ViewModel.TimeEntriesViewModel.CancelDeleteTimeEntry.Execute())
                .Show();
        }

        private void onWelcomeBackViewVisibilityChanged(bool shouldShowWelcomeBackView)
        {
            if (shouldShowWelcomeBackView)
            {
                if (welcomeBackView == null)
                {
                    welcomeBackView = welcomeBackStub.Inflate();
                }

                welcomeBackView.Visibility = ViewStates.Visible;
            }
            else if (welcomeBackView != null)
            {
                welcomeBackView.Visibility = ViewStates.Gone;
            }
        }

        private sealed class FabVisibilityListener : FloatingActionButton.OnVisibilityChangedListener
        {
            private readonly Action onFabHidden;

            public FabVisibilityListener(Action onFabHidden)
            {
                this.onFabHidden = onFabHidden;
            }

            public override void OnHidden(FloatingActionButton fab)
            {
                base.OnHidden(fab);
                onFabHidden();
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            DisposeBag?.Dispose();
        }
    }
}
