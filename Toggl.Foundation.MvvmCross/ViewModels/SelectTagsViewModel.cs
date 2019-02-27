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
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SelectTagsViewModel : MvxViewModel<(long[] tagIds, long workspaceId), long[]>
    {
        private readonly IInteractorFactory interactorFactory;
        private readonly IMvxNavigationService navigationService;
        private readonly IStopwatchProvider stopwatchProvider;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly HashSet<long> selectedTagIds = new HashSet<long>();

        private long[] defaultResult;
        private long workspaceId;
        private IStopwatch navigationFromEditTimeEntryStopwatch;

        public IObservable<IEnumerable<SelectableTagBaseViewModel>> Tags { get; private set; }
        public IObservable<bool> IsEmpty { get; private set; }
        public BehaviorSubject<string> FilterText { get; } = new BehaviorSubject<string>(String.Empty);
        public UIAction Close { get; }
        public UIAction Save { get; }

        public InputAction<SelectableTagBaseViewModel> SelectTag { get; }

        public SelectTagsViewModel(
            IMvxNavigationService navigationService,
            IStopwatchProvider stopwatchProvider,
            IInteractorFactory interactorFactory,
            ISchedulerProvider schedulerProvider,
            IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(stopwatchProvider, nameof(stopwatchProvider));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));
            Ensure.Argument.IsNotNull(schedulerProvider, nameof(schedulerProvider));

            this.navigationService = navigationService;
            this.stopwatchProvider = stopwatchProvider;
            this.interactorFactory = interactorFactory;
            this.schedulerProvider = schedulerProvider;

            Close = rxActionFactory.FromAsync(close);
            Save = rxActionFactory.FromAsync(save);
            SelectTag = rxActionFactory.FromAsync<SelectableTagBaseViewModel>(selectTag);
        }

        public override void Prepare((long[] tagIds, long workspaceId) parameter)
        {
            workspaceId = parameter.workspaceId;
            defaultResult = parameter.tagIds;
            selectedTagIds.AddRange(parameter.tagIds);
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            navigationFromEditTimeEntryStopwatch = stopwatchProvider.Get(MeasuredOperation.OpenSelectTagsView);
            stopwatchProvider.Remove(MeasuredOperation.OpenSelectTagsView);

            var filteredTags = FilterText
                .StartWith(string.Empty)
                .Select(text => text?.Trim() ?? string.Empty)
                .SelectMany(text => getSuggestions(text))
                .Select(pair =>
                {
                    var queryText = pair.Item1;
                    var suggestions = pair.Item2;

                    var tagSuggestionInWorkspace = suggestions
                        .Cast<TagSuggestion>()
                        .Where(s => s.WorkspaceId == workspaceId);

                    var suggestCreation = !string.IsNullOrEmpty(queryText)
                                          && tagSuggestionInWorkspace.None(tag
                                              => tag.Name.IsSameCaseInsensitiveTrimedTextAs(queryText))
                                          && queryText.IsAllowedTagByteSize();

                    var selectableViewModels = tagSuggestionInWorkspace
                        .OrderByDescending(tag => defaultResult.Contains(tag.TagId))
                        .ThenBy(tag => tag.Name)
                        .Select(toSelectableTagViewModel);

                    if (suggestCreation)
                    {
                        return selectableViewModels.Prepend(new SelectableTagCreationViewModel(queryText, workspaceId));
                    }

                    return selectableViewModels;
                })
                .ShareReplay();

            Tags = filteredTags
                .AsDriver(new SelectableTagBaseViewModel[0], schedulerProvider);

            IsEmpty = filteredTags
                .Select(tags => tags.Any())
                .Invert()
                .DistinctUntilChanged()
                .AsDriver(schedulerProvider);
        }

        public override void ViewAppeared()
        {
            base.ViewAppeared();
            navigationFromEditTimeEntryStopwatch?.Stop();
            navigationFromEditTimeEntryStopwatch = null;
        }

        private SelectableTagBaseViewModel toSelectableTagViewModel(TagSuggestion tagSuggestion)
            => new SelectableTagViewModel(
                tagSuggestion.TagId,
                tagSuggestion.Name,
                selectedTagIds.Contains(tagSuggestion.TagId),
                workspaceId);

        private IObservable<(string, IEnumerable<AutocompleteSuggestion>)> getSuggestions(string text)
        {
            var wordsToQuery = text.SplitToQueryWords();
            return interactorFactory
                .GetTagsAutocompleteSuggestions(wordsToQuery).Execute()
                .Select(suggestions => (text, suggestions));
        }

        private async Task selectTag(SelectableTagBaseViewModel tag)
        {
            switch (tag)
            {
                case SelectableTagCreationViewModel t:
                    var createdTag = await interactorFactory.CreateTag(t.Name, t.WorkspaceId).Execute();
                    selectedTagIds.Add(createdTag.Id);
                    FilterText.OnNext(string.Empty);
                    break;
                case SelectableTagViewModel t:
                    if (!selectedTagIds.Remove(t.Id))
                    {
                        selectedTagIds.Add(t.Id);
                    }

                    FilterText.OnNext(FilterText.Value);
                    break;
            }
        }

        private Task close()
            => navigationService.Close(this, defaultResult);

        private Task save() => navigationService.Close(this, selectedTagIds.ToArray());
    }
}
