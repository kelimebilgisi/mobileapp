using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
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
    public sealed class EditTimeEntryViewModel : MvxViewModel<long[]>
    {
        private const int maxTagLength = 30;

        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IDialogService dialogService;
        private readonly IInteractorFactory interactorFactory;
        private readonly IMvxNavigationService navigationService;
        private readonly IAnalyticsService analyticsService;
        private readonly IStopwatchProvider stopwatchProvider;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly IRxActionFactory actionFactory;
        public IOnboardingStorage OnboardingStorage { get; private set; }

        private IStopwatch stopwatchFromCalendar;
        private IStopwatch stopwatchFromMainLog;

        private long? projectId;
        private long? taskId;
        private BehaviorSubject<long> workspaceIdSubject;
        private IThreadSafeTimeEntry originalTimeEntry;

        public long[] TimeEntryIds { get; set; }
        public long TimeEntryId => TimeEntryIds.First();

        public bool IsEditingGroup => TimeEntryIds.Length > 1;
        public int GroupCount => TimeEntryIds.Length;

        private CompositeDisposable disposeBag = new CompositeDisposable();

        private BehaviorSubject<bool> isEditingDescriptionSubject;
        public BehaviorRelay<string> Description { get; private set; }

        private BehaviorSubject<ProjectClientTaskInfo> projectClientTaskSubject;
        public IObservable<ProjectClientTaskInfo> ProjectClientTask { get; private set; }

        public IObservable<bool> IsBillableAvailable { get; private set; }

        private BehaviorSubject<bool> isBillableSubject;
        public IObservable<bool> IsBillable { get; private set; }

        private BehaviorSubject<DateTimeOffset> startTimeSubject;
        public IObservable<DateTimeOffset> StartTime { get; private set; }

        private BehaviorSubject<TimeSpan?> durationSubject;
        public IObservable<TimeSpan> Duration { get; private set; }

        public IObservable<DateTimeOffset?> StopTime { get; private set; }

        public IObservable<bool> IsTimeEntryRunning { get; private set; }

        public TimeSpan GroupDuration { get; private set; }

        private BehaviorSubject<IEnumerable<IThreadSafeTag>> tagsSubject;
        public IObservable<IEnumerable<string>> Tags { get; set; }
        private IEnumerable<long> tagIds
            => tagsSubject.Value.Select(tag => tag.Id);

        private BehaviorSubject<bool> isInaccessibleSubject;
        public IObservable<bool> IsInaccessible { get; private set; }

        private BehaviorSubject<string> syncErrorMessageSubject;
        public IObservable<string> SyncErrorMessage { get; private set; }
        public IObservable<bool> IsSyncErrorMessageVisible { get; private set; }

        public IObservable<IThreadSafePreferences> Preferences { get; private set; }

        public UIAction Close { get; private set; }
        public UIAction SelectProject { get; private set; }
        public UIAction SelectTags { get; private set; }
        public UIAction ToggleBillable { get; private set; }
        public InputAction<EditViewTapSource> EditTimes { get; private set; }
        public UIAction StopTimeEntry { get; private set; }
        public UIAction DismissSyncErrorMessage { get; private set; }
        public UIAction Save { get; private set; }
        public UIAction Delete { get; private set; }

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

            projectClientTaskSubject = new BehaviorSubject<ProjectClientTaskInfo>(ProjectClientTaskInfo.Empty);
            ProjectClientTask = projectClientTaskSubject
                .AsDriver(ProjectClientTaskInfo.Empty, schedulerProvider);

            IsBillableAvailable = workspaceIdSubject
                .SelectMany(workspaceId => interactorFactory.IsBillableAvailableForWorkspace(workspaceId).Execute())
                .DistinctUntilChanged()
                .AsDriver(false, schedulerProvider);

            isBillableSubject = new BehaviorSubject<bool>(false);
            IsBillable = isBillableSubject
                .DistinctUntilChanged()
                .AsDriver(false, schedulerProvider);

            startTimeSubject = new BehaviorSubject<DateTimeOffset>(DateTimeOffset.UtcNow);
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

            projectClientTaskSubject.OnNext(new ProjectClientTaskInfo(
                timeEntry.Project?.DisplayName(),
                timeEntry.Project?.DisplayColor(),
                timeEntry.Project?.Client?.Name,
                timeEntry.Task?.Name));

            isBillableSubject.OnNext(timeEntry.Billable);

            startTimeSubject.OnNext(timeEntry.Start);

            durationSubject.OnNext(timeEntry.TimeSpanDuration());

            GroupDuration = timeEntries.Sum(entry => timeEntry.TimeSpanDuration());

            tagsSubject.OnNext(timeEntry.Tags?.ToImmutableList() ?? ImmutableList<IThreadSafeTag>.Empty);

            isInaccessibleSubject.OnNext(timeEntry.IsInaccessible);

            setupSyncError(timeEntries);

            Preferences = interactorFactory.GetPreferences().Execute()
                .AsDriver(null, schedulerProvider);
        }

        private void setupSyncError(IEnumerable<IThreadSafeTimeEntry> timeEntries)
        {
            var errorCount = timeEntries.Count(te => te.IsInaccessible || !string.IsNullOrEmpty(te.LastSyncErrorMessage));

            if (errorCount == 0)
                return;

            if (IsEditingGroup)
            {
                var message = string.Format(Resources.TimeEntriesGroupSyncErrorMessage, errorCount, TimeEntryIds.Length);
                syncErrorMessageSubject.OnNext(message);

                return;
            }

            var timeEntry = timeEntries.First();

            syncErrorMessageSubject.OnNext(
                timeEntry.IsInaccessible
                ? Resources.InaccessibleTimeEntryErrorMessage
                : timeEntry.LastSyncErrorMessage);
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
                projectClientTaskSubject.OnNext(ProjectClientTaskInfo.Empty);

                clearTagsIfNeeded(workspaceId, chosenProject.WorkspaceId);

                workspaceIdSubject.OnNext(chosenProject.WorkspaceId);

                return;
            }

            var project = await interactorFactory.GetProjectById(projectId.Value).Execute();
            clearTagsIfNeeded(workspaceId, project.WorkspaceId);

            var taskName = chosenProject.TaskId.HasValue
                ? (await interactorFactory.GetTaskById(taskId.Value).Execute())?.Name
                : string.Empty;

            projectClientTaskSubject.OnNext(new ProjectClientTaskInfo(
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

            var isDeletionConfirmed = await delete(actionType, TimeEntryIds.Length, interactor);

            if (isDeletionConfirmed)
                await close();
        }

        private async Task<bool> delete(ActionType actionType, int entriesCount, IInteractor<IObservable<Unit>> deletionInteractor)
        {
            var isDeletionConfirmed = await dialogService.ConfirmDestructiveAction(actionType, entriesCount);

            if (!isDeletionConfirmed)
                return false;

            await deletionInteractor.Execute();

            dataSource.SyncManager.InitiatePushSync();
            analyticsService.DeleteTimeEntry.Track();

            return true;
        }

        private Task close()
            => navigationService.Close(this);

        public struct ProjectClientTaskInfo
        {
            public ProjectClientTaskInfo(string project, string projectColor, string client, string task)
            {
                Project = project;
                ProjectColor = projectColor;
                Client = client;
                Task = task;
            }

            public string Project { get; private set; }
            public string ProjectColor { get; private set; }
            public string Client { get; private set; }
            public string Task { get; private set; }

            public bool HasProject => !string.IsNullOrEmpty(Project);

            public static ProjectClientTaskInfo Empty
                => new ProjectClientTaskInfo(null, null, null, null);
        }
    }
}
