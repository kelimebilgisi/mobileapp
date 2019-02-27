using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.FSharp.Core;
using NSubstitute;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Extensions;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.TestExtensions;
using Toggl.Multivac.Extensions;
using Xunit;
using Unit = System.Reactive.Unit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SelectTagsViewModelTests
    {
        public abstract class SelectTagsViewModelTest : BaseViewModelTests<SelectTagsViewModel>
        {
            protected override SelectTagsViewModel CreateViewModel()
                => new SelectTagsViewModel(
                    NavigationService,
                    StopwatchProvider,
                    InteractorFactory,
                    SchedulerProvider,
                    RxActionFactory
                );

            protected Task EnsureClosesTheViewModel()
                => NavigationService.Received().Close(Arg.Is(ViewModel), Arg.Any<long[]>());

            protected bool EnsureExpectedTagsAreReturned(long[] actual, long[] expected)
            {
                if (actual.Length != expected.Length) return false;

                foreach (var actualTag in actual)
                {
                    if (!expected.Contains(actualTag))
                        return false;
                }
                return true;
            }

            protected IEnumerable<TagSuggestion> CreateTags(int count)
                => Enumerable
                    .Range(0, count)
                    .Select(i => CreateTagSubstitute(i, i.ToString()))
                    .Select(tag => new TagSuggestion(tag));

            protected IThreadSafeTag CreateTagSubstitute(long id, string name)
            {
                var tag = Substitute.For<IThreadSafeTag>();
                tag.Id.Returns(id);
                tag.Name.Returns(name);
                return tag;
            }
        }

        public sealed class TheConstructor : SelectTagsViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useNavigationService,
                bool useInteractorFactory,
                bool useStopwatchProvider,
                bool useRxActionFactory,
                bool useSchedulerProvider)
            {
                var navigationService = useNavigationService ? NavigationService : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var stopwatchProvider = useStopwatchProvider ? StopwatchProvider : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SelectTagsViewModel(
                        navigationService,
                        stopwatchProvider,
                        interactorFactory,
                        schedulerProvider,
                        rxActionFactory);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheCloseAction : SelectTagsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.Initialize();

                ViewModel.Close.Execute();
                TestScheduler.Start();

                await EnsureClosesTheViewModel();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheSameTagsThatWerePassedToTheViewModel()
            {
                await ViewModel.Initialize();
                var tagids = new long[] { 1, 4, 29, 2 };
                ViewModel.Prepare((tagids, 0));

                ViewModel.Close.Execute();
                TestScheduler.Start();

                await NavigationService
                    .Received()
                    .Close(Arg.Is(ViewModel), Arg.Is<long[]>(tagids));
            }
        }

        public sealed class TheSaveCommand : SelectTagsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.Initialize();

                ViewModel.Save.Execute();
                TestScheduler.Start();

                await EnsureClosesTheViewModel();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheSelectedTagIds()
            {
                await ViewModel.Initialize();
                var tagIds = Enumerable.Range(0, 20).Select(num => (long)num);
                var selectedTagIds = tagIds.Where(id => id % 2 == 0)
                    .ToArray();
                var selectedTags = selectedTagIds
                    .Select(createDatabaseTagSubstitute)
                    .Select(databaseTag => new TagSuggestion(databaseTag))
                    .Select(tagSuggestion
                        => new SelectableTagViewModel(tagSuggestion.TagId, tagSuggestion.Name, false,
                            tagSuggestion.WorkspaceId));

                var auxObservable = TestScheduler.CreateObserver<Unit>();

                var selectExecutions = Observable.Concat(selectedTags
                    .Select(tag => Observable.Defer(() => ViewModel.SelectTag.Execute(tag))));

                var saveExecution = Observable.Defer(() => ViewModel.Save.Execute());

                Observable
                    .Concat(selectExecutions, saveExecution)
                    .Subscribe(auxObservable);

                TestScheduler.Start();

                await NavigationService
                    .Received()
                    .Close(
                        Arg.Is(ViewModel),
                        Arg.Is<long[]>(ids => EnsureExpectedTagsAreReturned(ids, selectedTagIds))
                    );
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsEmptyArrayIfNoTagsWereSelected()
            {
                await ViewModel.Initialize();
                var expectedIds = new long[0];

                ViewModel.Save.Execute();

                await NavigationService
                    .Received()
                    .Close(
                        Arg.Is(ViewModel),
                        Arg.Is<long[]>(ids => EnsureExpectedTagsAreReturned(ids, expectedIds))
                    );
            }

            private IThreadSafeTag createDatabaseTagSubstitute(long id)
            {
                var tag = Substitute.For<IThreadSafeTag>();
                tag.Id.Returns(id);
                tag.Name.Returns($"Tag{id}");
                return tag;
            }
        }

        public sealed class TheFilterTextProperty : SelectTagsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task WhenChangedQueriesTheAutocompleteProvider()
            {
                var text = "Some text";
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<IEnumerable<SelectableTagBaseViewModel>>();
                ViewModel.Tags.Subscribe(observer);

                ViewModel.FilterText.OnNext(text);
                TestScheduler.Start();

                InteractorFactory.Received()
                    .GetTagsAutocompleteSuggestions(
                        Arg.Is<IList<string>>(words => words.SequenceEqual(text.SplitToQueryWords())));
            }
        }

        public sealed class TheIsEmptyProperty : SelectTagsViewModelTest
        {
            const long workspaceId = 1;
            const long irrelevantWorkspaceId = 2;

            private void setup(Func<long, long> workspaceIdSelector)
            {
                var tags = Enumerable.Range(0, 10)
                                     .Select(i =>
                                     {
                                         var tag = Substitute.For<IThreadSafeTag>();
                                         tag.Name.Returns(Guid.NewGuid().ToString());
                                         tag.Id.Returns(i);
                                         tag.WorkspaceId.Returns(workspaceIdSelector(i));
                                         return tag;
                                     })
                                     .ToList();

                InteractorFactory
                    .GetTagsAutocompleteSuggestions(Arg.Any<IList<string>>())
                    .Execute()
                    .Returns(Observable.Return(TagSuggestion.FromTags(tags)));
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTrueIfHasNoTagsForSelectedWorkspace()
            {
                setup(i => irrelevantWorkspaceId);

                ViewModel.Prepare((new long[] { }, workspaceId));
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsEmpty.Subscribe(observer);

                TestScheduler.Start();

                observer.LastEmittedValue().Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfTagsForWorkspaceExist()
            {
                setup(i => i % 2 == 0 ? irrelevantWorkspaceId : workspaceId);

                ViewModel.Prepare((new long[] { }, workspaceId));
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsEmpty.Subscribe(observer);

                TestScheduler.Start();

                observer.LastEmittedValue().Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfTagsForWorkspaceExistButFilteredCollectionIsEmpty()
            {
                setup(i => i % 2 == 0 ? irrelevantWorkspaceId : workspaceId);

                var autocompleteProvider = Substitute.For<IAutocompleteProvider>();

                autocompleteProvider
                    .Query(Arg.Is<QueryInfo>(
                        arg => arg.SuggestionType == AutocompleteSuggestionType.Tags))
                    .Returns(Observable.Return(new List<TagSuggestion>()));

                ViewModel.Prepare((new long[] { }, workspaceId));
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<bool>();
                ViewModel.IsEmpty.Subscribe(observer);
                ViewModel.FilterText.OnNext("Anything");
                TestScheduler.Start();

                observer.LastEmittedValue().Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsFalseIfTagIsCreated()
            {
                InteractorFactory
                    .GetTagsAutocompleteSuggestions(Arg.Any<IList<string>>())
                    .Execute()
                    .Returns(Observable.Return(new List<AutocompleteSuggestion>()));

                var newTag = Substitute.For<IThreadSafeTag>();
                newTag.Id.Returns(12345);
                newTag.WorkspaceId.Returns(workspaceId);
                newTag.Name.Returns("new tag");

                ViewModel.Prepare((new long[] { }, workspaceId));

                var observer = TestScheduler.CreateObserver<bool>();
                await ViewModel.Initialize();
                ViewModel.IsEmpty.Subscribe(observer);
                InteractorFactory
                    .GetTagsAutocompleteSuggestions(Arg.Any<IList<string>>())
                    .Execute()
                    .Returns(Observable.Return(TagSuggestion.FromTags(new List<IThreadSafeTag> { newTag })));
                ViewModel.FilterText.OnNext(string.Empty);
                TestScheduler.Start();

                observer.LastEmittedValue().Should().BeFalse();
            }
        }

        public sealed class TheTagsProperty : SelectTagsViewModelTest
        {
            private IEnumerable<TagSuggestion> getTagSuggestions(int count, IThreadSafeWorkspace workspace)
            {
                for (int i = 0; i < count; i++)
                {
                    /* Do not inline 'workspace.Id' into another .Return() call
                     * because it's a proxy that won't work later on!
                     * This must be cached before usage.
                     */
                    var workspaceId = workspace.Id;

                    var tag = Substitute.For<IThreadSafeTag>();
                    tag.Id.Returns(i);
                    tag.WorkspaceId.Returns(workspaceId);
                    tag.Workspace.Returns(workspace);
                    tag.Name.Returns($"Tag{i}");

                    yield return new TagSuggestion(tag);
                }
            }

            private IThreadSafeWorkspace createWorkspace(long id, string name)
            {
                var workspace = Substitute.For<IThreadSafeWorkspace>();
                workspace.Id.Returns(id);
                workspace.Name.Returns(name);
                return workspace;
            }

            [Fact, LogIfTooSlow]
            public async Task OnlyContainsTagsFromTheSameWorkspaceAsTimeEntry()
            {
                var tags = new List<TagSuggestion>();
                var workspaces = Enumerable.Range(0, 5)
                    .Select(i => createWorkspace(i, $"Workspace{i}")).ToArray();
                workspaces.ForEach(workspace
                    => tags.AddRange(getTagSuggestions(5, workspace)));
                InteractorFactory.GetTagsAutocompleteSuggestions(Arg.Any<IList<string>>())
                    .Execute()
                    .Returns(Observable.Return(tags));
                var targetWorkspace = workspaces[1];
                InteractorFactory.GetWorkspaceById(targetWorkspace.Id).Execute()
                    .Returns(Observable.Return(targetWorkspace));
                var tagIds = tags.Select(tag => tag.TagId).ToArray();

                ViewModel.Prepare((tagIds, targetWorkspace.Id));
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<IEnumerable<SelectableTagBaseViewModel>>();
                ViewModel.Tags.Subscribe(observer);
                TestScheduler.Start();

                observer.LastEmittedValue().Should().HaveCount(5);
                observer.LastEmittedValue().Should()
                    .OnlyContain(tag => tag.WorkspaceId == targetWorkspace.Id);
            }

            [Fact, LogIfTooSlow]
            public async Task IsPopulatedAfterInitialization()
            {
                var workspace = createWorkspace(13, "Some workspace");
                var tagSuggestions = getTagSuggestions(10, workspace);
                var tagIds = tagSuggestions.Select(tag => tag.TagId).ToArray();
                InteractorFactory.GetTagsAutocompleteSuggestions(Arg.Any<IList<string>>())
                    .Execute()
                    .Returns(Observable.Return(tagSuggestions));
                InteractorFactory.GetWorkspaceById(workspace.Id).Execute()
                    .Returns(Observable.Return(workspace));

                ViewModel.Prepare((tagIds, workspace.Id));
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<IEnumerable<SelectableTagBaseViewModel>>();
                ViewModel.Tags.Subscribe(observer);
                TestScheduler.Start();

                observer.LastEmittedValue().Should().HaveCount(tagSuggestions.Count());
            }

            [Fact, LogIfTooSlow]
            public async Task IsSortedBySelectedStatusThenByName()
            {
                var workspace = createWorkspace(13, "Some workspace");
                var tagSuggestions = getTagSuggestions(4, workspace).ToArray();

                var shuffledTags = new[] { tagSuggestions[3], tagSuggestions[1], tagSuggestions[2], tagSuggestions[0] };
                var selectedTagIds = new[] { tagSuggestions[0].TagId, tagSuggestions[2].TagId };
                InteractorFactory.GetTagsAutocompleteSuggestions(Arg.Any<IList<string>>())
                    .Execute()
                    .Returns(Observable.Return(shuffledTags));
                InteractorFactory.GetWorkspaceById(workspace.Id).Execute()
                    .Returns(Observable.Return(workspace));

                ViewModel.Prepare((selectedTagIds, workspace.Id));
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<IEnumerable<SelectableTagBaseViewModel>>();
                ViewModel.Tags.Subscribe(observer);
                TestScheduler.Start();

                var tags = observer.LastEmittedValue().ToArray();
                tags.Should().HaveCount(4);


                tags[0].Name.Should().Be("Tag0");
                tags[1].Name.Should().Be("Tag2");
                tags[2].Name.Should().Be("Tag1");
                tags[3].Name.Should().Be("Tag3");

                tags[0].Selected.Should().BeTrue();
                tags[1].Selected.Should().BeTrue();
                tags[2].Selected.Should().BeFalse();
                tags[3].Selected.Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task IsUpdatedWhenTextIsChanged()
            {
                var workspace = createWorkspace(13, "Some workspace");
                var oldSuggestions = getTagSuggestions(3, workspace);
                var newSuggestions = getTagSuggestions(1, workspace);
                var oldTagIds = oldSuggestions.Select(tag => tag.TagId).ToArray();
                var queryText = "Query text";
                InteractorFactory.GetTagsAutocompleteSuggestions(
                        Arg.Is<IList<string>>(words => words.SequenceEqual(queryText.SplitToQueryWords())))
                    .Execute()
                    .Returns(Observable.Return(newSuggestions));
                InteractorFactory.GetWorkspaceById(workspace.Id).Execute()
                    .Returns(Observable.Return(workspace));
                ViewModel.Prepare((oldTagIds, workspace.Id));
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<IEnumerable<SelectableTagBaseViewModel>>();
                ViewModel.Tags.Subscribe(observer);
                ViewModel.FilterText.OnNext(queryText);
                TestScheduler.Start();

                var received = observer.LastEmittedValue();
                received.Should().HaveCount(2);
            }

            [Fact, LogIfTooSlow]
            public async Task AddCreationModelWhenNoMatchingSuggestion()
            {
                var workspace = createWorkspace(13, "Some workspace");
                var tagSuggestions = getTagSuggestions(10, workspace);
                InteractorFactory.GetTagsAutocompleteSuggestions(Arg.Any<IList<string>>())
                    .Execute()
                    .Returns(Observable.Return(tagSuggestions));
                InteractorFactory.GetWorkspaceById(workspace.Id).Execute()
                    .Returns(Observable.Return(workspace));
                var tagIds = tagSuggestions.Select(tag => tag.TagId).ToArray();

                var nonExistingTag = "Non existing tag";
                ViewModel.Prepare((tagIds, workspace.Id));
                await ViewModel.Initialize();
                var observer = TestScheduler.CreateObserver<IEnumerable<SelectableTagBaseViewModel>>();
                ViewModel.Tags.Subscribe(observer);
                ViewModel.FilterText.OnNext(nonExistingTag);
                TestScheduler.Start();

                observer.LastEmittedValue().First().Name.Should().Be(nonExistingTag);
                observer.LastEmittedValue().First().WorkspaceId.Should().Be(workspace.Id);
                observer.LastEmittedValue().First().Selected.Should().BeFalse();
                observer.LastEmittedValue().First().Should().BeOfType<SelectableTagCreationViewModel>();
            }
        }

        public sealed class TheSelectTagAction : SelectTagsViewModelTest
        {
            private TagSuggestion tagSuggestion;

            public TheSelectTagAction()
            {
                var databaseTag = Substitute.For<IThreadSafeTag>();
                databaseTag.Name.Returns("Tag0");
                databaseTag.Id.Returns(0);
                tagSuggestion = new TagSuggestion(databaseTag);
            }

            [Fact, LogIfTooSlow]
            public async Task CreatesANewTagWithTheGivenNameInTheCurrentWorkspace()
            {
                long workspaceId = 10;
                await ViewModel.Initialize();

                var newTag = new SelectableTagCreationViewModel("Some tag", workspaceId);
                ViewModel.Prepare();
                await ViewModel.Initialize();

                ViewModel.SelectTag.Execute(newTag);
                TestScheduler.Start();

                await InteractorFactory
                    .Received()
                    .CreateTag(Arg.Is(newTag.Name), Arg.Is(workspaceId))
                    .Execute();
            }

            [Fact, LogIfTooSlow]
            public async Task AppendsTheTagIdToSelectedTagIdsIfNotSelectedAlready()
            {
               var selectableTag = new SelectableTagViewModel(tagSuggestion.TagId, tagSuggestion.Name, true, 1);

               ViewModel.SelectTag.Execute(selectableTag);
               ViewModel.Save.Execute();
               TestScheduler.Start();
               await NavigationService
                   .Received()
                   .Close(
                       Arg.Is(ViewModel),
                       Arg.Is<long[]>(ids => EnsureExpectedTagsAreReturned(ids, new[] { selectableTag.Id }))
                   );
            }

            [Fact, LogIfTooSlow]
            public async Task RemovesTheTagIdFromSelectedTagIdsIfSelectedAlready()
            {
               var selectableTag = new SelectableTagViewModel(tagSuggestion.TagId, tagSuggestion.Name, true, 1);
               ViewModel.Prepare((new long[] { selectableTag.Id }, 0));

               ViewModel.SelectTag.Execute(selectableTag);
               ViewModel.Save.Execute();
               TestScheduler.Start();
               await NavigationService
                   .Received()
                   .Close(
                       Arg.Is(ViewModel),
                       Arg.Is<long[]>(ids => EnsureExpectedTagsAreReturned(ids, new long[0]))
                   );
            }
        }

        public sealed class ThePrepareMethod : SelectTagsViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task AddsAllPassedTagsToTheSelectedTags()
            {
                var tagIds = new long[] { 100, 3, 10, 34, 532 };

                ViewModel.Prepare((tagIds, 0));
                ViewModel.Save.Execute();

                await NavigationService
                    .Received()
                    .Close(
                        Arg.Is(ViewModel),
                        Arg.Is<long[]>(ids => EnsureExpectedTagsAreReturned(ids, tagIds))
                    );
            }
        }
    }
}

