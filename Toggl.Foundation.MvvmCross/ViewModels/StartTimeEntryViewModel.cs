﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using MvvmCross.Core.Navigation;
using MvvmCross.Core.ViewModels;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels.StartTimeEntrySuggestions;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class StartTimeEntryViewModel : MvxViewModel<DateParameter>
    {
        private const char projectQuerySymbol = '@';
        private readonly char[] querySymbols = { projectQuerySymbol };

        //Fields
        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IMvxNavigationService navigationService;
        private readonly Subject<(IEnumerable<string> WordsToQuery, SuggestionType SuggestionType)> querySubject
            = new Subject<(IEnumerable<string>, SuggestionType)>();

        private IDisposable queryDisposable;
        private IDisposable elapsedTimeDisposable;

        //Properties
        public long? ProjectId { get; private set; }

        public bool IsSuggestingProjects { get; set; }

        public TextFieldInfo TextFieldInfo { get; set; } = new TextFieldInfo("", 0);

        public TimeSpan ElapsedTime { get; private set; } = TimeSpan.Zero;

        public bool IsBillable { get; private set; } = false;

        public bool IsEditingProjects { get; private set; } = false;

        public bool IsEditingTags { get; private set; } = false;

        public DateTimeOffset StartDate { get; private set; }

        public DateTimeOffset? EndDate { get; private set; }

        public MvxObservableCollection<BaseTimeEntrySuggestionViewModel> Suggestions { get; }
            = new MvxObservableCollection<BaseTimeEntrySuggestionViewModel>();

        public IMvxAsyncCommand BackCommand { get; }

        public IMvxAsyncCommand DoneCommand { get; }

        public IMvxCommand ToggleBillableCommand { get; }

        public IMvxCommand ToggleProjectSuggestionsCommand { get; }

        public IMvxCommand<BaseTimeEntrySuggestionViewModel> SelectSuggestionCommand { get; }

        public StartTimeEntryViewModel(ITogglDataSource dataSource, ITimeService timeService, IMvxNavigationService navigationService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));

            this.dataSource = dataSource;
            this.timeService = timeService;
            this.navigationService = navigationService;

            BackCommand = new MvxAsyncCommand(back);
            DoneCommand = new MvxAsyncCommand(done);
            ToggleBillableCommand = new MvxCommand(toggleBillable);
            ToggleProjectSuggestionsCommand = new MvxCommand(toggleProjectSuggestions);
            SelectSuggestionCommand = new MvxCommand<BaseTimeEntrySuggestionViewModel>(selectSuggestion);
        }

        private void selectSuggestion(BaseTimeEntrySuggestionViewModel suggestion)
        {
            switch (suggestion)
            {
                case TimeEntrySuggestionViewModel timeEntrySuggestion:
                    
                    var description = timeEntrySuggestion.Description;

                    ProjectId = timeEntrySuggestion.ProjectId;
                    TextFieldInfo = new TextFieldInfo(
                        description,
                        description.Length,
                        timeEntrySuggestion.ProjectName,
                        timeEntrySuggestion.ProjectColor
                    );

                    break;

                case ProjectSuggestionViewModel projectSuggestion:
                    
                    removeProjectQueryFromDescriptionIfNeeded();

                    ProjectId = projectSuggestion.ProjectId;
                    TextFieldInfo = TextFieldInfo.WithProjectInfo(
                        projectSuggestion.ProjectName, 
                        projectSuggestion.ProjectColor
                    );
                    break;
            }
        }

        public override async Task Initialize(DateParameter parameter)
        {
            await Initialize();

            StartDate = parameter.GetDate();

            elapsedTimeDisposable =
                timeService.CurrentDateTimeObservable.Subscribe(currentTime => ElapsedTime = currentTime - StartDate);

            queryDisposable = querySubject.AsObservable()
                .DistinctUntilChanged()
                .SelectMany(querySuggestions)
                .Subscribe(onSuggestions);
        }
        
        private void OnTextFieldInfoChanged()
        {
            if (string.IsNullOrEmpty(TextFieldInfo.ProjectName))
                ProjectId = null;

            var (queryText, suggestionType) = parseQuery(TextFieldInfo);

            var wordsToQuery = queryText.Split(' ').Where(word => !string.IsNullOrEmpty(word)).Distinct();
            querySubject.OnNext((wordsToQuery, suggestionType));
        }

        private (string, SuggestionType) parseQuery(TextFieldInfo info)
        {
            if (string.IsNullOrEmpty(TextFieldInfo.Text) || ProjectId != null) 
                return (info.Text, SuggestionType.TimeEntries);

            var stringToSearch = info.Text.Substring(0, info.DescriptionCursorPosition);
            var indexOfQuerySymbol = stringToSearch.LastIndexOfAny(querySymbols);
            if (indexOfQuerySymbol >= 0)
            {
                var startingIndex = indexOfQuerySymbol + 1;
                var stringLength = info.Text.Length - indexOfQuerySymbol - 1;
                return (info.Text.Substring(startingIndex, stringLength), SuggestionType.Projects);
            }

            return (info.Text, SuggestionType.TimeEntries);
        }

        private void toggleProjectSuggestions()
        {
            if (IsSuggestingProjects)
            {
                removeProjectQueryFromDescriptionIfNeeded();
                return;
            }

            if (ProjectId != null)
            {
                querySubject.OnNext((Enumerable.Empty<string>(), SuggestionType.Projects));
                return;
            }

            var newText = TextFieldInfo.Text.Insert(TextFieldInfo.CursorPosition, "@");
            TextFieldInfo = new TextFieldInfo(newText, TextFieldInfo.CursorPosition + 1);
        }

        private void toggleBillable() => IsBillable = !IsBillable;

        private Task back() => navigationService.Close(this);

        private async Task done()
        {
            await dataSource.TimeEntries.Start(StartDate, TextFieldInfo.Text, IsBillable, ProjectId);

            await navigationService.Close(this);
        }

        private void removeProjectQueryFromDescriptionIfNeeded()
        {
            var indexOfProjectQuerySymbol = TextFieldInfo.Text.IndexOf(projectQuerySymbol);
            if (indexOfProjectQuerySymbol < 0) 
            {
                OnTextFieldInfoChanged();
                return;
            }

            var newText = TextFieldInfo.Text.Substring(0, indexOfProjectQuerySymbol);
            TextFieldInfo = new TextFieldInfo(newText, newText.Length);
        }
        
        private IObservable<IEnumerable<BaseTimeEntrySuggestionViewModel>> querySuggestions(
            (IEnumerable<string> WordsToQuery, SuggestionType SuggestionType) tuple)
        {
            var queryListIsEmpty = !tuple.WordsToQuery.Any();

            if (tuple.SuggestionType == SuggestionType.Projects)
            {
                IsSuggestingProjects = true;

                if (queryListIsEmpty)
                    return dataSource.Projects.GetAll()
                        .Select(ProjectSuggestionViewModel.FromProjectsPrependingEmpty);

                return tuple.WordsToQuery
                    .Aggregate(dataSource.Projects.GetAll(), (obs, word) => obs.Select(filterProjectsByWord(word)))
                    .Select(ProjectSuggestionViewModel.FromProjects);
            }

            IsSuggestingProjects = false;

            if (queryListIsEmpty)
                return Observable.Return(Enumerable.Empty<BaseTimeEntrySuggestionViewModel>());

            return tuple.WordsToQuery
               .Aggregate(dataSource.TimeEntries.GetAll(), (obs, word) => obs.Select(filterTimeEntriesByWord(word)))
               .Select(TimeEntrySuggestionViewModel.FromTimeEntries);
        }

        private void onSuggestions(IEnumerable<BaseTimeEntrySuggestionViewModel> suggestions)
        {
            Suggestions.Clear();
            Suggestions.AddRange(suggestions.Distinct(SuggestionComparer.Instance));
        }

        private Func<IEnumerable<IDatabaseTimeEntry>, IEnumerable<IDatabaseTimeEntry>> filterTimeEntriesByWord(string word)
            => timeEntries => 
                timeEntries.Where(
                    te => te.Description.ContainsIgnoringCase(word)
                       || (te.Project != null && te.Project.Name.ContainsIgnoringCase(word))
                       || (te.Project?.Client != null && te.Project.Client.Name.ContainsIgnoringCase(word)));

        private Func<IEnumerable<IDatabaseProject>, IEnumerable<IDatabaseProject>> filterProjectsByWord(string word)
            => projects =>
                projects.Where(
                    p => p.Name.ContainsIgnoringCase(word)
                      || (p.Client != null && p.Client.Name.ContainsIgnoringCase(word)));
    }
}
