using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Android.Support.V7.Widget;
using Android.Widget;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Onboarding.MainView;
using Toggl.Foundation.Sync;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Giskard.Helper;
using Toggl.Giskard.ViewHolders;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Extensions;
using Toggl.PrimeRadiant.Onboarding;
using static Toggl.Giskard.ViewHolders.MainLogCellViewHolder;

namespace Toggl.Giskard.Activities
{
    public partial class MainActivity
    {
        private PopupWindow playButtonTooltipPopupWindow;
        private PopupWindow stopButtonTooltipPopupWindow;
        private PopupWindow tapToEditPopup;
        private PopupWindow swipeRightPopup;
        private PopupWindow swipeLeftPopup;

        private DismissableOnboardingStep swipeRightOnboardingStep;
        private IDisposable swipeRightOnboardingStepDisposable;
        private IDisposable swipeRightOnboardingAnimationStepDisposable;
        private IDisposable swipeToContinueWasUsedDisposable;

        private DismissableOnboardingStep swipeLeftOnboardingStep;
        private IDisposable swipeLeftOnboardingStepDisposable;
        private IDisposable swipeLeftOnboardingAnimationStepDisposable;
        private IDisposable swipeToDeleteWasUsedDisposable;

        private readonly BehaviorSubject<int> timeEntriesCountSubject = new BehaviorSubject<int>(0);
        private EditTimeEntryOnboardingStep editTimeEntryOnboardingStep;
        private IDisposable editTimeEntryOnboardingStepDisposable;

        private IObservable<Unit> mainRecyclerViewChangesObservable;
        private ISubject<Unit> mainRecyclerViewScrollChanges = new Subject<Unit>();
        private IDisposable mainRecyclerViewScrollChangesDisposable;

        protected override void OnResume()
        {
            base.OnResume();
            mainRecyclerViewScrollChangesDisposable = mainRecyclerView
                .Rx()
                .OnScrolled()
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(mainRecyclerViewScrollChanges.OnNext);
        }

        protected override void OnPause()
        {
            base.OnPause();
            mainRecyclerViewScrollChangesDisposable?.Dispose();
        }

        protected override void OnStop()
        {
            base.OnStop();
            playButtonTooltipPopupWindow.Dismiss();
            stopButtonTooltipPopupWindow.Dismiss();
        }

        private void setupOnboardingSteps()
        {
            setupMainLogObservables();
            setupStartTimeEntryOnboardingStep();
            setupStopTimeEntryOnboardingStep();
            setupTapToEditOnboardingStep();
            setupSwipeRightOnboardingStep();
            setupSwipeLeftOnboardingStep();
        }

        private void setupMainLogObservables()
        {
            var collectionChanges = ViewModel.TimeEntries.SelectUnit();
            mainRecyclerViewChangesObservable = mainRecyclerViewScrollChanges
                .Merge(collectionChanges);
        }

        private void setupStartTimeEntryOnboardingStep()
        {
            if (playButtonTooltipPopupWindow == null)
            {
                playButtonTooltipPopupWindow = PopupWindowFactory.PopupWindowWithText(
                    this,
                    Resource.Layout.TooltipWithRightArrow,
                    Resource.Id.TooltipText,
                    Resource.String.OnboardingTapToStartTimer);
            }

            new StartTimeEntryOnboardingStep(ViewModel.OnboardingStorage)
                .ManageDismissableTooltip(
                    playButtonTooltipPopupWindow,
                    playButton,
                    (popup, anchor) => popup.LeftVerticallyCenteredOffsetsTo(anchor, dpExtraRightMargin: 8),
                    ViewModel.OnboardingStorage)
                .DisposedBy(DisposeBag);
        }

        private void setupStopTimeEntryOnboardingStep()
        {
            if (stopButtonTooltipPopupWindow == null)
            {
                stopButtonTooltipPopupWindow = PopupWindowFactory.PopupWindowWithText(
                    this,
                    Resource.Layout.TooltipWithRightBottomArrow,
                    Resource.Id.TooltipText,
                    Resource.String.OnboardingTapToStopTimer);
            }

            new StopTimeEntryOnboardingStep(ViewModel.OnboardingStorage, ViewModel.IsTimeEntryRunning)
                .ManageDismissableTooltip(
                    stopButtonTooltipPopupWindow,
                    stopButton,
                    (popup, anchor) => popup.TopRightFrom(anchor, dpExtraBottomMargin: 8),
                    ViewModel.OnboardingStorage)
                .DisposedBy(DisposeBag);
        }

        private void setupTapToEditOnboardingStep()
        {
            tapToEditPopup = PopupWindowFactory.PopupWindowWithText(
                this,
                Resource.Layout.TooltipWithLeftTopArrow,
                Resource.Id.TooltipText,
                Resource.String.OnboardingTapToEdit);

            editTimeEntryOnboardingStep = new EditTimeEntryOnboardingStep(
                ViewModel.OnboardingStorage, Observable.Return(false));

            var showTapToEditOnboardingStepObservable =
                Observable.CombineLatest(
                    editTimeEntryOnboardingStep.ShouldBeVisible,
                    mainRecyclerViewChangesObservable,
                    ViewModel.SyncProgressState,
                    (shouldShowStep, unit, syncState) => shouldShowStep && syncState == SyncProgress.Synced);

            showTapToEditOnboardingStepObservable
                .Where(shouldShowStep => shouldShowStep)
                .Select(_ => findOldestTimeEntryView())
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(updateTapToEditOnboardingStep)
                .DisposedBy(DisposeBag);
        }

        private void updateTapToEditOnboardingStep(MainLogCellViewHolder oldestVisibleTimeEntryViewHolder)
        {
            tapToEditPopup?.Dismiss();

            if (oldestVisibleTimeEntryViewHolder == null)
                return;

            if (editTimeEntryOnboardingStepDisposable != null)
            {
                editTimeEntryOnboardingStepDisposable.Dispose();
                editTimeEntryOnboardingStepDisposable = null;
            }

            editTimeEntryOnboardingStepDisposable = editTimeEntryOnboardingStep
                .ManageVisibilityOf(
                    tapToEditPopup,
                    oldestVisibleTimeEntryViewHolder.ItemView,
                    (window, view) => PopupOffsets.FromDp(16, -4, this));
        }

        private void setupSwipeRightOnboardingStep()
        {
            var shouldBeVisible = editTimeEntryOnboardingStep
                .ShouldBeVisible
                .Select(visible => !visible);

            var showSwipeRightOnboardingStep = Observable.CombineLatest(
                shouldBeVisible,
                mainRecyclerViewChangesObservable,
                ViewModel.SyncProgressState,
                (shouldShowStep, unit, syncState) => shouldShowStep && syncState == SyncProgress.Synced);


            swipeRightPopup = PopupWindowFactory.PopupWindowWithText(
                this,
                Resource.Layout.TooltipWithLeftTopArrow,
                Resource.Id.TooltipText,
                Resource.String.OnboardingSwipeRight);

            swipeRightOnboardingStep = new SwipeRightOnboardingStep(shouldBeVisible, timeEntriesCountSubject.AsObservable())
                .ToDismissable(nameof(SwipeRightOnboardingStep), ViewModel.OnboardingStorage);
            swipeRightOnboardingStep.DismissByTapping(swipeRightPopup, () =>
            {
                if (swipeRightOnboardingAnimationStepDisposable != null)
                {
                    swipeRightOnboardingAnimationStepDisposable.Dispose();
                    swipeRightOnboardingAnimationStepDisposable = null;
                }
            });

            swipeToContinueWasUsedDisposable = mainRecyclerAdapter.ContinueTimeEntrySubject
                .Subscribe(_ =>
                {
                    swipeRightOnboardingStep.Dismiss();
                    swipeToContinueWasUsedDisposable.Dispose();
                    swipeToContinueWasUsedDisposable = null;
                });

            showSwipeRightOnboardingStep
                .Where(shouldShowStep => shouldShowStep)
                .Select(_ => findEarliestTimeEntryView())
                .DistinctUntilChanged()
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(updateSwipeRightOnboardingStep)
                .DisposedBy(DisposeBag);
        }

        private void updateSwipeRightOnboardingStep(MainLogCellViewHolder lastTimeEntry)
        {
            swipeRightPopup?.Dismiss();

            if (lastTimeEntry == null)
                return;

            if (swipeRightOnboardingStepDisposable != null)
            {
                swipeRightOnboardingStepDisposable.Dispose();
                swipeRightOnboardingStepDisposable = null;
            }

            swipeRightOnboardingStepDisposable = swipeRightOnboardingStep
                .ManageVisibilityOf(
                    swipeRightPopup,
                    lastTimeEntry.ItemView,
                    (window, view) => PopupOffsets.FromDp(16, -4, this));

            if (swipeRightOnboardingAnimationStepDisposable != null)
            {
                swipeRightOnboardingAnimationStepDisposable.Dispose();
                swipeRightOnboardingAnimationStepDisposable = null;
            }

            swipeRightOnboardingAnimationStepDisposable = swipeRightOnboardingStep
                .ManageSwipeActionAnimationOf(mainRecyclerView, lastTimeEntry, AnimationSide.Right);
        }

        private void setupSwipeLeftOnboardingStep()
        {
            var shouldBeVisible = Observable.CombineLatest(
                editTimeEntryOnboardingStep.ShouldBeVisible,
                swipeRightOnboardingStep.ShouldBeVisible,
                (editTimeEntryVisible, swipeRightVisible) => !editTimeEntryVisible && !swipeRightVisible
            );

            var showSwipeLeftOnboardingStep = Observable.CombineLatest(
                shouldBeVisible,
                mainRecyclerViewChangesObservable,
                ViewModel.SyncProgressState,
                (shouldShowStep, unit, syncState) => shouldShowStep && syncState == SyncProgress.Synced);

            swipeLeftPopup = PopupWindowFactory.PopupWindowWithText(
                this,
                Resource.Layout.TooltipWithRightTopArrow,
                Resource.Id.TooltipText,
                Resource.String.OnboardingSwipeLeft);

            swipeLeftOnboardingStep = new SwipeLeftOnboardingStep(shouldBeVisible, timeEntriesCountSubject.AsObservable())
                .ToDismissable(nameof(SwipeLeftOnboardingStep), ViewModel.OnboardingStorage);

            swipeLeftOnboardingStep.DismissByTapping(swipeLeftPopup, () =>
            {
                if (swipeLeftOnboardingAnimationStepDisposable != null)
                {
                    swipeLeftOnboardingAnimationStepDisposable.Dispose();
                    swipeLeftOnboardingAnimationStepDisposable = null;
                }
            });

            swipeToDeleteWasUsedDisposable = mainRecyclerAdapter.DeleteTimeEntrySubject
                .Subscribe(_ =>
                {
                    swipeLeftOnboardingStep.Dismiss();
                    swipeToDeleteWasUsedDisposable.Dispose();
                    swipeToDeleteWasUsedDisposable = null;
                });

            showSwipeLeftOnboardingStep
                .Where(shouldShowStep => shouldShowStep)
                .Select(_ => findEarliestTimeEntryView())
                .DistinctUntilChanged()
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(updateSwipeLeftOnboardingStep)
                .DisposedBy(DisposeBag);
        }

        private void updateSwipeLeftOnboardingStep(MainLogCellViewHolder lastTimeEntry)
        {
            swipeLeftPopup?.Dismiss();

            if (lastTimeEntry == null)
                return;

            if (swipeLeftOnboardingStepDisposable != null)
            {
                swipeLeftOnboardingStepDisposable.Dispose();
                swipeLeftOnboardingStepDisposable = null;
            }

            swipeLeftOnboardingStepDisposable = swipeLeftOnboardingStep
                .ManageVisibilityOf(
                    swipeLeftPopup,
                    lastTimeEntry.ItemView,
                    (window, view) => window.BottomRightOffsetsTo(view, -16, -4));

            if (swipeLeftOnboardingAnimationStepDisposable != null)
            {
                swipeLeftOnboardingAnimationStepDisposable.Dispose();
                swipeLeftOnboardingAnimationStepDisposable = null;
            }

            swipeLeftOnboardingAnimationStepDisposable = swipeLeftOnboardingStep
                .ManageSwipeActionAnimationOf(mainRecyclerView, lastTimeEntry, AnimationSide.Left);
        }

        private MainLogCellViewHolder findOldestTimeEntryView()
        {
            if (mainRecyclerAdapter == null)
            {
                return null;
            }

            for (var position = layoutManager.FindLastVisibleItemPosition(); position >= 0; position--)
            {
                var viewType = mainRecyclerAdapter.GetItemViewType(position);
                if (viewType != MainRecyclerAdapter.ItemViewType)
                {
                    continue;
                }

                var viewHolder = findLogCellViewHolderAtPosition(position);
                if (viewHolder == null)
                    return null;

                return isVisible(viewHolder)
                    ? viewHolder
                    : null;
            }

            return null;
        }

        private MainLogCellViewHolder findEarliestTimeEntryView()
        {
            var position = mainRecyclerAdapter.HeaderOffset + 1;
            var viewHolder = findLogCellViewHolderAtPosition(position);

            if (viewHolder == null)
            {
                return null;
            }

            return isVisible(viewHolder) ? viewHolder : null;
        }

        private MainLogCellViewHolder findLogCellViewHolderAtPosition(int position)
        {
            var viewHolder = mainRecyclerView.FindViewHolderForLayoutPosition(position);

            if (viewHolder == null)
                return null;

            if (viewHolder is MainLogCellViewHolder logViewHolder)
                return logViewHolder;

            return null;
        }

        private bool isVisible(RecyclerView.ViewHolder view)
        {
            return layoutManager.IsViewPartiallyVisible(view.ItemView, true, true)
                   || layoutManager.IsViewPartiallyVisible(view.ItemView, false, true);
        }
    }
}
