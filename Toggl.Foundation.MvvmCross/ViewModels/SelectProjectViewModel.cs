using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using PropertyChanged;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using static Toggl.Foundation.Helper.Constants;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    using WorkspaceGroupedSuggestionsCollection = WorkspaceGroupedCollection<AutocompleteSuggestion>;

    [Preserve(AllMembers = true)]
    public sealed class SelectProjectViewModel
        : MvxViewModel<SelectProjectParameter, SelectProjectParameter>
    {
        private readonly ITogglDataSource dataSource;
        private readonly IDialogService dialogService;
        private readonly IInteractorFactory interactorFactory;
        private readonly IMvxNavigationService navigationService;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly IStopwatchProvider stopwatchProvider;
        private readonly Subject<string> infoSubject = new Subject<string>();

        private long? taskId;
        private long? projectId;
        private long workspaceId;
        private IStopwatch navigationFromEditTimeEntryViewModelStopwatch;

        public string Text { get; set; } = "";

        [DependsOn(nameof(Text))]
        public bool UsesFilterOld => !string.IsNullOrEmpty(Text.Trim());

        [DependsOn(nameof(Text))]
        public bool SuggestCreationOld
        {
            get
            {
                if (!shouldShowProjectCreationSuggestion)
                    return false;

                if (!UsesFilterOld)
                    return false;

                var text = Text.Trim();

                var isOfAllowedLength = text.LengthInBytes() <= MaxProjectNameLengthInBytes;
                if (!isOfAllowedLength)
                    return false;

                var hasNoExactMatches = SuggestionsOld.None(ws => ws.Any(s => s is ProjectSuggestion ps && ps.ProjectName == text));
                return hasNoExactMatches;
            }
        }

        public bool IsEmptyOld { get; set; } = false;

        public IMvxAsyncCommand CreateProjectCommand { get; }

        public IMvxCommand<ProjectSuggestion> ToggleTaskSuggestionsCommand { get; }

        public IMvxAsyncCommand<AutocompleteSuggestion> SelectProjectCommand { get; }

        public NestableObservableCollection<WorkspaceGroupedCollection<AutocompleteSuggestion>, AutocompleteSuggestion> SuggestionsOld { get; }
            = new NestableObservableCollection<WorkspaceGroupedCollection<AutocompleteSuggestion>, AutocompleteSuggestion>();

        /*
         * The new stuff goes below
         */
        private List<IThreadSafeWorkspace> allWorkspaces = new List<IThreadSafeWorkspace>();
        private bool shouldShowProjectCreationSuggestion;

        public bool UseGrouping { get; private set; }

        public ObservableGroupedOrderedCollection<AutocompleteSuggestion> Suggestions { get; }

        public ISubject<string> FilterText { get; } = new BehaviorSubject<string>(string.Empty);

        public IObservable<bool> IsEmpty { get; }

        public IObservable<bool> SuggestCreation { get; }

        public IObservable<bool> UsesFilter { get; }

        public IObservable<string> PlaceholderText { get; }

        public UIAction Close { get; }

        public InputAction<ProjectSuggestion> ToggleTaskSuggestions { get; }

        public InputAction<AutocompleteSuggestion> SelectProject { get; }

        public SelectProjectViewModel(
            ITogglDataSource dataSource,
            IRxActionFactory rxActionFactory,
            IInteractorFactory interactorFactory,
            IMvxNavigationService navigationService,
            IDialogService dialogService,
            ISchedulerProvider schedulerProvider,
            IStopwatchProvider stopwatchProvider)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(dialogService, nameof(dialogService));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));
            Ensure.Argument.IsNotNull(stopwatchProvider, nameof(stopwatchProvider));

            this.dataSource = dataSource;
            this.dialogService = dialogService;
            this.interactorFactory = interactorFactory;
            this.navigationService = navigationService;
            this.schedulerProvider = schedulerProvider;
            this.stopwatchProvider = stopwatchProvider;

            CreateProjectCommand = new MvxAsyncCommand(createProject);
            SelectProjectCommand = new MvxAsyncCommand<AutocompleteSuggestion>(selectProject);
            ToggleTaskSuggestionsCommand = new MvxCommand<ProjectSuggestion>(toggleTaskSuggestions);

            Suggestions = new ObservableGroupedOrderedCollection<AutocompleteSuggestion>(
                indexKey: getIndexKey,
                orderingKey: getOrderingKey,
                groupingKey: suggestion => suggestion.WorkspaceId
            );

            Close = rxActionFactory.FromAsync(close);
            ToggleTaskSuggestions = rxActionFactory.FromAction<ProjectSuggestion>(toggleTaskSuggestions);
            SelectProject = rxActionFactory.FromAsync<AutocompleteSuggestion>(selectProject);

            IsEmpty = dataSource.Projects.GetAll().Select(projects => projects.None());
            PlaceholderText = IsEmpty.Select(isEmpty => isEmpty ? Resources.EnterProject : Resources.AddFilterProjects);
            SuggestCreation = FilterText.Select(shouldSuggestCreation);

            FilterText.Select(text => text.SplitToQueryWords())
                      .ObserveOn(schedulerProvider.BackgroundScheduler)
                      .SelectMany(query => interactorFactory.GetProjectsAutocompleteSuggestions(query).Execute())
                      .SubscribeOn(schedulerProvider.MainScheduler)
                      .Select(suggestions => suggestions.Cast<ProjectSuggestion>())
                      .Select(setSelectedProject)
                      .Subscribe(Suggestions.ReplaceWith);
        }

        private IComparable getIndexKey(AutocompleteSuggestion autocompleteSuggestion)
        {
            if (autocompleteSuggestion is ProjectSuggestion projectSuggestion)
                return projectSuggestion.ProjectId;
            if (autocompleteSuggestion is TaskSuggestion taskSuggestion)
                return taskSuggestion.TaskId;

            throw new Exception($"Unexpected {nameof(AutocompleteSuggestion)} encountered in ${nameof(Suggestions)}");
        }

        private IComparable getOrderingKey(AutocompleteSuggestion autocompleteSuggestion)
        {
            if (autocompleteSuggestion is ProjectSuggestion projectSuggestion)
                return projectSuggestion.ProjectName;
            if (autocompleteSuggestion is TaskSuggestion taskSuggestion)
                return $"{taskSuggestion.ProjectName}{taskSuggestion.Name}";

            throw new Exception($"Unexpected {nameof(AutocompleteSuggestion)} encountered in ${nameof(Suggestions)}");
        }

        public override void Prepare(SelectProjectParameter parameter)
        {
            taskId = parameter.TaskId;
            projectId = parameter.ProjectId;
            workspaceId = parameter.WorkspaceId;
        }

        public override async Task Initialize()
        {
            await base.Initialize();
            navigationFromEditTimeEntryViewModelStopwatch = stopwatchProvider.Get(MeasuredOperation.OpenSelectProjectFromEditView);
            stopwatchProvider.Remove(MeasuredOperation.OpenSelectProjectFromEditView);

            var workspaces = await interactorFactory.GetAllWorkspaces().Execute();

            shouldShowProjectCreationSuggestion = workspaces.Any(ws => ws.IsEligibleForProjectCreation());
            allWorkspaces = workspaces.ToList();
            UseGrouping = allWorkspaces.Count > 1;

            dataSource.Projects
                      .GetAll()
                      .Select(projects => projects.Any())
                      .Subscribe(hasProjects => IsEmptyOld = !hasProjects);
        }

        public override void ViewAppeared()
        {
            base.ViewAppeared();
            navigationFromEditTimeEntryViewModelStopwatch?.Stop();
            navigationFromEditTimeEntryViewModelStopwatch = null;
        }

        private IEnumerable<ProjectSuggestion> setSelectedProject(IEnumerable<ProjectSuggestion> suggestions)
        {
            return suggestions.Select(s =>
            {
                s.Selected = s.ProjectId == projectId;
                return s;
            });
        }

        private void OnTextChanged()
        {
            infoSubject.OnNext(Text);
        }

        private IEnumerable<WorkspaceGroupedSuggestionsCollection> addMissingWorkspacesTo(IEnumerable<WorkspaceGroupedSuggestionsCollection> workspaces)
        {
            var usedWorkspaceIds = new HashSet<long>(workspaces.Select(ws => ws.WorkspaceId));

            var unusedWorkspaces = allWorkspaces
                .Where(ws => !usedWorkspaceIds.Contains(ws.Id))
                .Select(workspaceGroupedSuggestionCollection);

            return workspaces.Concat(unusedWorkspaces);
        }

        private WorkspaceGroupedSuggestionsCollection workspaceGroupedSuggestionCollection(IThreadSafeWorkspace workspace)
            => new WorkspaceGroupedSuggestionsCollection(
                workspace.Name,
                workspace.Id,
                new[] { ProjectSuggestion.NoProject(workspace.Id, workspace.Name) });

        private IEnumerable<WorkspaceGroupedSuggestionsCollection> groupByWorkspace(IEnumerable<ProjectSuggestion> suggestions, bool prependNoProjectItem)
        {
            var sortedSuggestions = suggestions.OrderBy(ps => ps.ProjectName);

            var groupedSuggestions = prependNoProjectItem
                ? sortedSuggestions.GroupByWorkspaceAddingNoProject()
                : sortedSuggestions.GroupByWorkspace();

            return groupedSuggestions;
        }

        private async Task createProject()
        {
            if (!SuggestCreationOld) return;

            var createdProjectId = await navigationService.Navigate<EditProjectViewModel, string, long?>(Text.Trim());
            if (createdProjectId == null) return;

            var project = await interactorFactory.GetProjectById(createdProjectId.Value).Execute();
            var parameter = SelectProjectParameter.WithIds(project.Id, null, project.WorkspaceId);
            await navigationService.Close(this, parameter);
        }

        private Task close()
            => navigationService.Close(
                this,
                SelectProjectParameter.WithIds(projectId, taskId, workspaceId));

        private async Task selectProject(AutocompleteSuggestion suggestion)
        {
            if (suggestion.WorkspaceId == workspaceId || suggestion.WorkspaceId == 0)
            {
                setProject(suggestion);
                return;
            }

            var shouldSetProject = await dialogService.Confirm(
                Resources.DifferentWorkspaceAlertTitle,
                Resources.DifferentWorkspaceAlertMessage,
                Resources.Ok,
                Resources.Cancel
            );

            if (!shouldSetProject) return;

            setProject(suggestion);
        }

        private void setProject(AutocompleteSuggestion suggestion)
        {
            workspaceId = suggestion.WorkspaceId;
            switch (suggestion)
            {
                case ProjectSuggestion projectSuggestion:
                    projectId = projectSuggestion
                        .ProjectId == 0 ? null : (long?)projectSuggestion.ProjectId;
                    taskId = null;
                    break;

                case TaskSuggestion taskSuggestion:
                    projectId = taskSuggestion.ProjectId;
                    taskId = taskSuggestion.TaskId;
                    break;

                default:
                    throw new ArgumentException($"{nameof(suggestion)} must be either of type {nameof(ProjectSuggestion)} or {nameof(TaskSuggestion)}.");
            }

            navigationService.Close(
                this,
                SelectProjectParameter.WithIds(projectId, taskId, workspaceId));
        }

        private void toggleTaskSuggestions(ProjectSuggestion projectSuggestion)
        {
            var grouping = Suggestions.FirstOrDefault(g => g.FirstOrDefault()?.WorkspaceId == projectSuggestion.WorkspaceId);

            if (grouping == null) return;
            if (!grouping.Contains(projectSuggestion)) return;

            if (!projectSuggestion.TasksVisible)
            {
                projectSuggestion.TasksVisible = true;
                Suggestions.AddItems(projectSuggestion.Tasks);
            }
            else
            {
                projectSuggestion.TasksVisible = false;
                Suggestions.RemoveItems(projectSuggestion.Tasks);
            }
        }

        private IEnumerable<AutocompleteSuggestion> getSuggestionsWithTasks(
            IEnumerable<AutocompleteSuggestion> suggestions)
        {
            foreach (var suggestion in suggestions)
            {
                if (suggestion is TaskSuggestion) continue;

                yield return suggestion;

                if (suggestion is ProjectSuggestion projectSuggestion && projectSuggestion.TasksVisible)
                {
                    var orderedTasks = projectSuggestion.Tasks
                        .OrderBy(t => t.Name);

                    foreach (var taskSuggestion in orderedTasks)
                        yield return taskSuggestion;
                }
            }
        }

        private bool shouldSuggestCreation(string text)
        {
            if (!shouldShowProjectCreationSuggestion)
                return false;

            if (!UsesFilterOld)
                return false;

            text = text.Trim();

            var isOfAllowedLength = text.LengthInBytes() <= MaxProjectNameLengthInBytes;
            if (!isOfAllowedLength)
                return false;


            //TODO: don't use old Suggestions list
            var hasNoExactMatches = SuggestionsOld.None(ws => ws.Any(s => s is ProjectSuggestion ps && ps.ProjectName == text));
            return hasNoExactMatches;
        }
    }
}
