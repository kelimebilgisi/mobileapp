using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Extensions.Reactive;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed partial class EditTimeEntryViewModel : MvxViewModel<long[]>
    {

        public EditTimeEntryViewModel(
            ITimeService timeService,
            ITogglDataSource dataSource,
            IInteractorFactory interactorFactory,
            IMvxNavigationService navigationService,
            IOnboardingStorage onboardingStorage,
            IDialogService dialogService,
            IAnalyticsService analyticsService,
            IStopwatchProvider stopwatchProvider,
            IRxActionFactory actionFactory,
            ISchedulerProvider schedulerProvider)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(onboardingStorage, nameof(onboardingStorage));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(analyticsService, nameof(analyticsService));
            Ensure.Argument.IsNotNull(stopwatchProvider, nameof(stopwatchProvider));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(actionFactory, nameof(actionFactory));

            this.dataSource = dataSource;
            this.timeService = timeService;
            this.dialogService = dialogService;
            this.interactorFactory = interactorFactory;
            this.navigationService = navigationService;
            this.analyticsService = analyticsService;
            this.stopwatchProvider = stopwatchProvider;
            this.schedulerProvider = schedulerProvider;
            this.actionFactory = actionFactory;
            OnboardingStorage = onboardingStorage;

            workspaceIdSubject = new BehaviorSubject<long>(0);

            isEditingDescriptionSubject = new BehaviorSubject<bool>(false);
            Description = new BehaviorRelay<string>(string.Empty, CommonFunctions.Trim);

            projectClientTaskSubject = new BehaviorSubject<(string, string, string, string)>(
                (null, null, null, null));
            HasProject = projectClientTaskSubject
                .Select(data => !string.IsNullOrEmpty(data.Project))
                .DistinctUntilChanged()
                .AsDriver(false, schedulerProvider);
            Project = projectClientTaskSubject
                .Select(data => (data.Project, data.ProjectColor))
                .DistinctUntilChanged()
                .AsDriver((null, null), schedulerProvider);
            Client = projectClientTaskSubject
               .Select(data => data.Client)
               .DistinctUntilChanged()
               .AsDriver(null, schedulerProvider);
            Task = projectClientTaskSubject
                .Select(data => data.Task)
                .DistinctUntilChanged()
                .AsDriver(null, schedulerProvider);

            IsBillableAvailable = workspaceIdSubject
                .SelectMany(workspaceId => interactorFactory.IsBillableAvailableForWorkspace(workspaceId).Execute())
                .DistinctUntilChanged()
                .AsDriver(false, schedulerProvider);

            isBillableSubject = new BehaviorSubject<bool>(false);
            IsBillable = isBillableSubject
                .DistinctUntilChanged()
                .AsDriver(false, schedulerProvider);

            StartTime = startTimeSubject
                .DistinctUntilChanged()
                .AsDriver(default(DateTimeOffset), schedulerProvider);

            durationSubject = new BehaviorSubject<TimeSpan?>(null);
            Duration = Observable
                .CombineLatest(
                    StartTime, durationSubject, timeService.CurrentDateTimeObservable,
                    calculateDisplayedDuration)
                .DistinctUntilChanged()
                .AsDriver(TimeSpan.Zero, schedulerProvider);

            StopTime = Observable.CombineLatest(
                StartTime, durationSubject, calculateStopTime)
                .DistinctUntilChanged()
                .AsDriver(null, schedulerProvider);

            IsTimeEntryRunning = StopTime
                .Select(stopTime => !stopTime.HasValue)
                .DistinctUntilChanged()
                .AsDriver(false, schedulerProvider);

            tagsSubject = new BehaviorSubject<IEnumerable<IThreadSafeTag>>(Enumerable.Empty<IThreadSafeTag>());
            Tags = tagsSubject
                .Select(tags => tags.Select(ellipsize).ToImmutableList())
                .AsDriver(ImmutableList<string>.Empty, schedulerProvider);

            isInaccessibleSubject = new BehaviorSubject<bool>(false);
            IsInaccessible = isInaccessibleSubject
                .DistinctUntilChanged()
                .AsDriver(false, schedulerProvider);

            syncErrorMessageSubject = new BehaviorSubject<string>(string.Empty);
            SyncErrorMessage = syncErrorMessageSubject
                .Select(error => error ?? string.Empty)
                .DistinctUntilChanged()
                .AsDriver(string.Empty, schedulerProvider);

            IsSyncErrorMessageVisible = syncErrorMessageSubject
                .Select(error => !string.IsNullOrEmpty(error))
                .DistinctUntilChanged()
                .AsDriver(false, schedulerProvider);

            preferencesSubject = new BehaviorSubject<IThreadSafePreferences>(null);

            // Actions
            Close = actionFactory.FromAsync(closeWithConfirmation);
            SelectProject = actionFactory.FromAsync(selectProject);
            SelectTags = actionFactory.FromAsync(selectTags);
            ToggleBillable = actionFactory.FromAction(toggleBillable);
            EditTimes = actionFactory.FromAsync<EditViewTapSource>(editTimes);
            StopTimeEntry = actionFactory.FromAction(stopTimeEntry);
            DismissSyncErrorMessage = actionFactory.FromAction(dismissSyncErrorMessage);
            Save = actionFactory.FromAsync(save);
            Delete = actionFactory.FromAsync(delete);
        }

        public override void Prepare(long[] parameter)
        {
            TimeEntryIds = parameter;
        }

        public override async Task Initialize()
        {
            stopwatchFromCalendar = stopwatchProvider.Get(MeasuredOperation.EditTimeEntryFromCalendar);
            stopwatchProvider.Remove(MeasuredOperation.EditTimeEntryFromCalendar);
            stopwatchFromMainLog = stopwatchProvider.Get(MeasuredOperation.EditTimeEntryFromMainLog);
            stopwatchProvider.Remove(MeasuredOperation.EditTimeEntryFromMainLog);

            var timeEntries = await interactorFactory.GetMultipleTimeEntriesById(TimeEntryIds).Execute();
            var timeEntry = timeEntries.First();
            originalTimeEntry = timeEntry;

            projectId = timeEntry.Project?.Id;
            taskId = timeEntry.Task?.Id;
            workspaceIdSubject.OnNext(timeEntry.WorkspaceId);

            Description.Accept(timeEntry.Description);

            projectClientTaskSubject.OnNext(
                (timeEntry.Project?.Name,
                timeEntry.Project?.Color,
                timeEntry.Project?.Client?.Name,
                timeEntry.Task?.Name));

            isBillableSubject.OnNext(timeEntry.Billable);

            startTimeSubject.OnNext(timeEntry.Start);

            durationSubject.OnNext(timeEntry.TimeSpanDuration());

            GroupDuration = timeEntries.Sum(entry => timeEntry.TimeSpanDuration());

            tagsSubject.OnNext(timeEntry.Tags?.ToImmutableList() ?? ImmutableList<IThreadSafeTag>.Empty);

            isInaccessibleSubject.OnNext(timeEntry.IsInaccessible);

            syncErrorMessageSubject.OnNext(
                timeEntry.IsInaccessible
                ? Resources.InaccessibleTimeEntryErrorMessage
                : timeEntry.LastSyncErrorMessage);

            interactorFactory.GetPreferences().Execute()
                .Subscribe(preferencesSubject)
                .DisposedBy(disposeBag);

            interactorFactory.GetCurrentUser().Execute()
                .Select(user => user.BeginningOfWeek)
                .Subscribe(beginningOfWeekSubject)
                .DisposedBy(disposeBag);
        }

        public override void ViewAppeared()
        {
            base.ViewAppeared();

            stopwatchFromCalendar?.Stop();
            stopwatchFromCalendar = null;

            stopwatchFromMainLog?.Stop();
            stopwatchFromMainLog = null;
        }

        public override void ViewDestroy(bool viewFinishing)
        {
            base.ViewDestroy(viewFinishing);

            disposeBag?.Dispose();
        }

        private TimeSpan calculateDisplayedDuration(DateTimeOffset start, TimeSpan? duration, DateTimeOffset currentTime)
            => duration ?? (currentTime - start);

        private DateTimeOffset? calculateStopTime(DateTimeOffset start, TimeSpan? duration)
            => duration.HasValue ? start + duration : null;

        private static string ellipsize(IThreadSafeTag tag)
        {
            var tagLength = tag.Name.LengthInGraphemes();
            if (tagLength <= maxTagLength)
                return tag.Name;

            return $"{tag.Name.UnicodeSafeSubstring(0, maxTagLength)}...";
        }

        private async Task selectProject()
        {
            analyticsService.EditEntrySelectProject.Track();
            analyticsService.EditViewTapped.Track(EditViewTapSource.Project);

            OnboardingStorage.SelectsProject();

            var selectProjectStopwatch = stopwatchProvider.CreateAndStore(
                MeasuredOperation.OpenSelectProjectFromEditView, true);

            selectProjectStopwatch.Start();

            var workspaceId = workspaceIdSubject.Value;

            var chosenProject = await navigationService
                .Navigate<SelectProjectViewModel, SelectProjectParameter, SelectProjectParameter>(
                    SelectProjectParameter.WithIds(projectId, taskId, workspaceId));

            if (chosenProject.WorkspaceId == workspaceId
                && chosenProject.ProjectId == projectId
                && chosenProject.TaskId == taskId)
                return;

            projectId = chosenProject.ProjectId;
            taskId = chosenProject.TaskId;

            if (projectId == null)
            {
                projectClientTaskSubject.OnNext(
                    (string.Empty, string.Empty, string.Empty, string.Empty));

                clearTagsIfNeeded(workspaceId, chosenProject.WorkspaceId);

                workspaceIdSubject.OnNext(chosenProject.WorkspaceId);

                return;
            }

            var project = await interactorFactory.GetProjectById(projectId.Value).Execute();
            clearTagsIfNeeded(workspaceId, project.WorkspaceId);

            var taskName = chosenProject.TaskId.HasValue
                ? (await interactorFactory.GetTaskById(taskId.Value).Execute()).Name
                : string.Empty;

            projectClientTaskSubject.OnNext((
                project.DisplayName(),
                project.DisplayColor(),
                project.Client?.Name,
                taskName));

            workspaceIdSubject.OnNext(chosenProject.WorkspaceId);
        }

        private void clearTagsIfNeeded(long currentWorkspaceId, long newWorkspaceId)
        {
            if (currentWorkspaceId == newWorkspaceId)
                return;

            tagsSubject.OnNext(ImmutableList<IThreadSafeTag>.Empty);
        }

        private async Task selectTags()
        {
            analyticsService.EditEntrySelectTag.Track();
            analyticsService.EditViewTapped.Track(EditViewTapSource.Tags);
            stopwatchProvider.CreateAndStore(MeasuredOperation.OpenSelectTagsView).Start();

            var workspaceId = workspaceIdSubject.Value;

            var currentTags = tagIds.OrderBy(CommonFunctions.Identity).ToArray();

            var chosenTags = await navigationService
                .Navigate<SelectTagsViewModel, (long[], long), long[]>((currentTags, workspaceId));

            if (chosenTags.OrderBy(CommonFunctions.Identity).SequenceEqual(currentTags))
                return;

            var tags = await interactorFactory.GetMultipleTagsById(chosenTags).Execute();

            tagsSubject.OnNext(tags);
        }

        private void toggleBillable()
        {
            analyticsService.EditViewTapped.Track(EditViewTapSource.Billable);

            isBillableSubject.OnNext(!isBillableSubject.Value);
        }

        private async Task editTimes(EditViewTapSource tapSource)
        {
            analyticsService.EditViewTapped.Track(tapSource);

            var isDurationInitiallyFocused = tapSource == EditViewTapSource.Duration;

            var duration = durationSubject.Value;
            var startTime = startTimeSubject.Value;
            var currentDuration = DurationParameter.WithStartAndDuration(startTime, duration);
            var editDurationParam = new EditDurationParameters(currentDuration, false, isDurationInitiallyFocused);

            var selectedDuration = await navigationService
                .Navigate<EditDurationViewModel, EditDurationParameters, DurationParameter>(editDurationParam)
                .ConfigureAwait(false);

            startTimeSubject.OnNext(selectedDuration.Start);
            if (selectedDuration.Duration.HasValue)
                durationSubject.OnNext(selectedDuration.Duration);
        }

        private void stopTimeEntry()
        {
            var duration = timeService.CurrentDateTime - startTimeSubject.Value;
            durationSubject.OnNext(duration);
        }

        private void dismissSyncErrorMessage()
        {
            syncErrorMessageSubject.OnNext(null);
        }

        private async Task closeWithConfirmation()
        {
            if (isDirty)
            {
                var userConfirmedDiscardingChanges = await dialogService.ConfirmDestructiveAction(ActionType.DiscardEditingChanges);

                if (!userConfirmedDiscardingChanges)
                    return;
            }

            await navigationService.Close(this);
        }

        private bool isDirty
            => originalTimeEntry == null
            || originalTimeEntry.Description != Description.Value
            || originalTimeEntry.WorkspaceId != workspaceIdSubject.Value
            || originalTimeEntry.ProjectId != projectId
            || originalTimeEntry.TaskId != taskId
            || originalTimeEntry.Start != startTimeSubject.Value
            || !originalTimeEntry.TagIds.SetEquals(tagIds)
            || originalTimeEntry.Billable != isBillableSubject.Value
            || originalTimeEntry.Duration != (long?)durationSubject.Value?.TotalSeconds;

        private async Task save()
        {
            OnboardingStorage.EditedTimeEntry();

            var timeEntries = await interactorFactory.GetMultipleTimeEntriesById(TimeEntryIds).Execute();

            var commonTimeEntryData = new EditTimeEntryDto
            {
                Id = TimeEntryIds.First(),
                Description = Description.Value?.Trim() ?? string.Empty,
                StartTime = startTimeSubject.Value,
                StopTime = calculateStopTime(startTimeSubject.Value, durationSubject.Value),
                ProjectId = projectId,
                TaskId = taskId,
                Billable = isBillableSubject.Value,
                WorkspaceId = workspaceIdSubject.Value,
                TagIds = tagIds.ToArray()
            };

            var timeEntriesDtos = timeEntries
                .Select(timeEntry => applyDataFromTimeEntry(commonTimeEntryData, timeEntry))
                .ToArray();

            interactorFactory
                .UpdateMultipleTimeEntries(timeEntriesDtos)
                .Execute()
                .SubscribeToErrorsAndCompletion((Exception ex) => close(), () => close())
                .DisposedBy(disposeBag);
        }

        private EditTimeEntryDto applyDataFromTimeEntry(EditTimeEntryDto commonTimeEntryData, IThreadSafeTimeEntry timeEntry)
        {
            commonTimeEntryData.Id = timeEntry.Id;
            commonTimeEntryData.StartTime = timeEntry.Start;
            commonTimeEntryData.StopTime = calculateStopTime(timeEntry.Start, timeEntry.TimeSpanDuration());

            return commonTimeEntryData;
        }

        private async Task delete()
        {
            var actionType = IsEditingGroup
                ? ActionType.DeleteMultipleExistingTimeEntries
                : ActionType.DeleteExistingTimeEntry;

            var interactor = IsEditingGroup
                ? interactorFactory.DeleteMultipleTimeEntries(TimeEntryIds)
                : interactorFactory.DeleteTimeEntry(TimeEntryId);

            await delete(actionType, TimeEntryIds.Length, interactor);

            analyticsService.DeleteTimeEntry.Track();

            await close();
        }

        private async Task delete(ActionType actionType, int entriesCount, IInteractor<IObservable<Unit>> interactor)
        {
            var shouldDelete = await dialogService.ConfirmDestructiveAction(actionType, entriesCount);

            if (!shouldDelete)
                return;

            await interactor.Execute();

            dataSource.SyncManager.InitiatePushSync();
        }

        private Task close()
            => navigationService.Close(this);
    }
}
