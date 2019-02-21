using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Helper;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Transformations;
using Toggl.Foundation.MvvmCross.ViewModels.TimeEntriesLog;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{

    [Preserve(AllMembers = true)]
    public sealed class TimeEntriesViewModel
    {
        private readonly ITogglDataSource dataSource;
        private readonly IInteractorFactory interactorFactory;
        private readonly IAnalyticsService analyticsService;
        private readonly ISchedulerProvider schedulerProvider;

        private readonly TimeEntriesGroupsFlattening groupsFlatteningStrategy;

        private Subject<int?> timeEntriesPendingDeletionSubject = new Subject<int?>();
        private IDisposable delayedDeletionDisposable;
        private long[] timeEntriesToDelete;

        public IObservable<IEnumerable<CollectionSection<DaySummaryViewModel, LogItemViewModel>>> TimeEntries { get; }
        public IObservable<bool> Empty { get; }
        public IObservable<int> Count { get; }
        public IObservable<int?> TimeEntriesPendingDeletion { get; }

        public InputAction<long[]> DelayDeleteTimeEntries { get; }
        public InputAction<GroupId> ToggleGroupExpansion { get; }
        public UIAction CancelDeleteTimeEntry { get; }

        public TimeEntriesViewModel(
            ITogglDataSource dataSource,
            IInteractorFactory interactorFactory,
            IAnalyticsService analyticsService,
            ISchedulerProvider schedulerProvider,
            IRxActionFactory rxActionFactory,
            ITimeService timeService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));

            this.dataSource = dataSource;
            this.interactorFactory = interactorFactory;
            this.analyticsService = analyticsService;
            this.schedulerProvider = schedulerProvider;

            DelayDeleteTimeEntries = rxActionFactory.FromAction<long[]>(delayDeleteTimeEntries);
            ToggleGroupExpansion = rxActionFactory.FromAction<GroupId>(toggleGroupExpansion);
            CancelDeleteTimeEntry = rxActionFactory.FromAction(cancelDeleteTimeEntry);

            groupsFlatteningStrategy = new TimeEntriesGroupsFlattening(timeService, dataSource.Preferences.Current);

            var deletingOrPressingUndo = timeEntriesPendingDeletionSubject.SelectUnit().StartWith(Unit.Default);
            var collapsingOrExpanding = ToggleGroupExpansion.Elements.StartWith(Unit.Default);

            TimeEntries =
                interactorFactory.ObserveAllTimeEntriesVisibleToTheUser().Execute()
                    .Select(timeEntries => timeEntries.Where(isNotRunning))
                    .CombineLatest(
                        deletingOrPressingUndo,
                        (timeEntries, _) => timeEntries.Where(isNotDeleted))
                    .CombineLatest(dataSource.Preferences.Current, group)
                    .CombineLatest(collapsingOrExpanding, (groups, _) => groups)
                    .Select(groupsFlatteningStrategy.Flatten)
                    .AsDriver(schedulerProvider);

            Empty = TimeEntries
                .Select(groups => groups.None())
                .AsDriver(schedulerProvider);

            Count = TimeEntries
                .Select(log => log.Sum(day => day.Items.Count))
                .AsDriver(schedulerProvider);

            TimeEntriesPendingDeletion = timeEntriesPendingDeletionSubject.AsObservable().AsDriver(schedulerProvider);
        }

        public async Task FinalizeDelayDeleteTimeEntryIfNeeded()
        {
            if (timeEntriesToDelete == null)
            {
                return;
            }

            delayedDeletionDisposable.Dispose();
            await deleteTimeEntries(timeEntriesToDelete);
            timeEntriesToDelete = null;
            timeEntriesPendingDeletionSubject.OnNext(null);
        }

        private IEnumerable<CollectionSection<DateTimeOffset, IThreadSafeTimeEntry[]>> group(
            IEnumerable<IThreadSafeTimeEntry> timeEntries, IThreadSafePreferences preferences)
            => preferences.CollapseTimeEntries
                ? TimeEntriesGrouping.GroupSimilar(timeEntries)
                : TimeEntriesGrouping.WithoutGroupingSimilar(timeEntries);

        private void toggleGroupExpansion(GroupId groupId)
        {
            groupsFlatteningStrategy.ToggleGroupExpansion(groupId);
        }

        private void delayDeleteTimeEntries(long[] timeEntries)
        {
            timeEntriesToDelete = timeEntries;

            timeEntriesPendingDeletionSubject.OnNext(timeEntries.Length);

            delayedDeletionDisposable = Observable.Merge( // If 5 seconds pass or we try to delete another TE
                    Observable.Return(timeEntries).Delay(Constants.UndoTime, schedulerProvider.DefaultScheduler),
                    timeEntriesPendingDeletionSubject
                        .Where(numberOfDeletedTimeEntries => numberOfDeletedTimeEntries != null)
                        .SelectValue(timeEntries)
                )
                .Take(1)
                .SelectMany(deleteTimeEntries)
                .Do(deletedTimeEntries =>
                {
                    // Hide bar if there isn't other TE trying to be deleted
                    if (deletedTimeEntries == timeEntriesToDelete)
                    {
                        timeEntriesPendingDeletionSubject.OnNext(null);
                    }
                })
                .Subscribe();
        }

        private void cancelDeleteTimeEntry()
        {
            timeEntriesToDelete = null;
            delayedDeletionDisposable.Dispose();
            timeEntriesPendingDeletionSubject.OnNext(null);
        }

        private IObservable<long[]> deleteTimeEntries(long[] timeEntries)
        {
            var observables = timeEntries.Select(timeEntryId =>
                interactorFactory
                    .DeleteTimeEntry(timeEntryId)
                    .Execute()
                    .Do(_ =>
                    {
                        analyticsService.DeleteTimeEntry.Track();
                        dataSource.SyncManager.PushSync();
                    }));

            return observables.Merge().LastAsync().SelectValue(timeEntries);
        }

        private bool isNotRunning(IThreadSafeTimeEntry timeEntry) => !timeEntry.IsRunning();

        private bool isNotDeleted(IThreadSafeTimeEntry timeEntry)
            => timeEntriesToDelete == null || !timeEntriesToDelete.Contains(timeEntry.Id);
    }
}
