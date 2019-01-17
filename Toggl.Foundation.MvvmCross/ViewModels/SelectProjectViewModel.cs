using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
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

        private long? taskId;
        private long? projectId;
        private long workspaceId;
        private IStopwatch navigationFromEditTimeEntryViewModelStopwatch;

        private List<IThreadSafeWorkspace> allWorkspaces = new List<IThreadSafeWorkspace>();
        private bool shouldShowProjectCreationSuggestion;

        public bool UseGrouping { get; private set; }

        public ObservableGroupedOrderedCollection<AutocompleteSuggestion> Suggestions { get; }

        public ISubject<string> FilterText { get; } = new BehaviorSubject<string>(string.Empty);

        public IObservable<bool> IsEmpty { get; }

        private ISubject<CreateEntitySuggestion> createEntitySuggestionSubject = new Subject<CreateEntitySuggestion>();
        public IObservable<CreateEntitySuggestion> CreateEntitySuggestion { get; }

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

            CreateEntitySuggestion = createEntitySuggestionSubject.AsObservable();

            FilterText.Select(text => (text, text.SplitToQueryWords()))
                      .ObserveOn(schedulerProvider.BackgroundScheduler)
                      .Select(tuple => (tuple.Item1, interactorFactory.GetProjectsAutocompleteSuggestions(tuple.Item2).Execute().SelectMany(x => x).ToEnumerable()))
                      .SubscribeOn(schedulerProvider.MainScheduler)
                      .Select(tuple => (tuple.Item1, tuple.Item2.Cast<ProjectSuggestion>()))
                      .Select(tuple => (tuple.Item1, setSelectedProject(tuple.Item2)))
                      .Do(tuple => updateCreateEntitySuggestion(tuple.Item1, tuple.Item2))
                      .Subscribe(tuple => Suggestions.ReplaceWith(tuple.Item2));
        }

        private void updateCreateEntitySuggestion(string text, IEnumerable<AutocompleteSuggestion> suggestions)
        {
            CreateEntitySuggestion createEntitySuggestion = null;

            if (shouldSuggestCreation(text, suggestions))
                createEntitySuggestion = new CreateEntitySuggestion(Resources.CreateProject, text);

            createEntitySuggestionSubject.OnNext(createEntitySuggestion);
        }

        private bool shouldSuggestCreation(string text, IEnumerable<AutocompleteSuggestion> suggestions)
        {
            if (!shouldShowProjectCreationSuggestion)
                return false;

            text = text.Trim();

            if (string.IsNullOrEmpty(text))
                return false;

            var isOfAllowedLength = text.LengthInBytes() <= MaxProjectNameLengthInBytes;
            if (!isOfAllowedLength)
                return false;

            var hasNoExactMatches = suggestions.None(suggestion => suggestion is ProjectSuggestion ps && ps.ProjectName == text);
            return hasNoExactMatches;
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
        private async Task createProject(string name)
        {
            var createdProjectId = await navigationService.Navigate<EditProjectViewModel, string, long?>(name);
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
            if (suggestion is CreateEntitySuggestion createEntitySuggestion)
            {
                await createProject(createEntitySuggestion.EntityName);
                return;
            }

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
    }
}
