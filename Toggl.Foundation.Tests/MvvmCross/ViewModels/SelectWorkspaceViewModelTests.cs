using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.TestExtensions;
using Toggl.Multivac.Extensions;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SelectWorkspaceViewModelTests
    {
        public abstract class SelectWorkspaceViewModelTest : BaseViewModelTests<SelectWorkspaceViewModel>
        {
            protected override SelectWorkspaceViewModel CreateViewModel()
                => new SelectWorkspaceViewModel(InteractorFactory, NavigationService, RxActionFactory);

            protected List<IThreadSafeWorkspace> GenerateWorkspaceList() =>
                Enumerable.Range(0, 10).Select(i =>
                {
                    var workspace = Substitute.For<IThreadSafeWorkspace>();
                    workspace.Id.Returns(i);
                    workspace.Name.Returns(i.ToString());
                    workspace.OnlyAdminsMayCreateProjects.Returns(i < 5);
                    return workspace;
                }).ToList();
        }

        public sealed class TheConstructor : SelectWorkspaceViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useInteractorFactory,
                bool useNavigationService,
                bool useRxActionFactory)
            {
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var navigationService = useNavigationService ? NavigationService : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new SelectWorkspaceViewModel(interactorFactory, navigationService, rxActionFactory);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class ThePrepareMethod : SelectWorkspaceViewModelTest
        {
            public ThePrepareMethod()
            {
                var workspaces = GenerateWorkspaceList();
                InteractorFactory.GetAllWorkspaces().Execute().Returns(Observable.Return(workspaces));
            }

            [Fact, LogIfTooSlow]
            public async Task SetsTheDefaultWorkspaceId()
            {
                const long expectedId = 8;

                ViewModel.Prepare(expectedId);

                await ViewModel.Initialize();
                ViewModel.Workspaces.Single(x => x.Selected).WorkspaceId.Should().Be(expectedId);
            }
        }

        public sealed class TheTitleProperty : SelectWorkspaceViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void HasCorrectValue()
            {
                ViewModel.Title.Should().Be(Resources.SetDefaultWorkspace);
            }
        }

        public sealed class TheInitializeMethod : SelectWorkspaceViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task AddsEligibleWorkspacesToTheList()
            {
                var workspaces = GenerateWorkspaceList();
                var eligibleWorkspaces = workspaces.Where(ws => ws.IsEligibleForProjectCreation());

                InteractorFactory.GetAllWorkspaces().Execute().Returns(Observable.Return(workspaces));

                await ViewModel.Initialize();

                ViewModel.Workspaces.Should().HaveCount(eligibleWorkspaces.Count());
            }
        }

        public sealed class TheCloseAction : SelectWorkspaceViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                await ViewModel.Initialize();

                await ViewModel.Close.Execute(TestScheduler);

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Any<long>());
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheWorkspacePassedOnPrepare()
            {
                const long expectedId = 10;
                ViewModel.Prepare(expectedId);
                await ViewModel.Initialize();

                await ViewModel.Close.Execute(TestScheduler);

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), expectedId);
            }
        }

        public sealed class TheSelectWorkspaceAction : SelectWorkspaceViewModelTest
        {
            private readonly IThreadSafeWorkspace Workspace = Substitute.For<IThreadSafeWorkspace>();

            [Fact, LogIfTooSlow]
            public async Task ClosesTheViewModel()
            {
                var selectableWorkspace = new SelectableWorkspaceViewModel(Workspace, true);

               await ViewModel.SelectWorkspace.Execute(selectableWorkspace, TestScheduler);

                await NavigationService.Received()
                    .Close(Arg.Is(ViewModel), Arg.Any<long>());
            }

            [Fact, LogIfTooSlow]
            public async Task ReturnsTheSelectedWorkspaceId()
            {
                const long expectedId = 10;
                Workspace.Id.Returns(expectedId);
                Workspace.IsEligibleForProjectCreation().Returns(true);
                var selectableWorkspace = new SelectableWorkspaceViewModel(Workspace, true);

                await ViewModel.SelectWorkspace.Execute(selectableWorkspace, TestScheduler);

                await NavigationService.Received().Close(
                    Arg.Is(ViewModel),
                    Arg.Is(expectedId)
                );
            }
        }
    }
}
