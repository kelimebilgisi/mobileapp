using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Sync.States.CleanUp;
using Toggl.Foundation.Tests.Mocks;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States.CleanUp
{
    public sealed class CheckForInaccessibleWorkspacesTests
    {
        private readonly CheckForInaccessibleWorkspacesState state;

        private readonly ITogglDataSource dataSource = Substitute.For<ITogglDataSource>();

        public CheckForInaccessibleWorkspacesTests()
        {
            state = new CheckForInaccessibleWorkspacesState(dataSource);
        }

        [Theory, LogIfTooSlow]
        [InlineData(0, SyncStatus.InSync)]
        [InlineData(1, SyncStatus.InSync)]
        [InlineData(3, SyncStatus.InSync)]
        [InlineData(1, SyncStatus.SyncFailed)]
        [InlineData(3, SyncStatus.SyncFailed)]
        public async Task ReturnsTheCorrectStateBasedOnTheNumberOfInaccessibleWorkspaces(int numberOfInaccessibleWorkspaces, SyncStatus syncStatus)
        {
            var workspaces = new List<MockWorkspace>
            {
                new MockWorkspace { Id = 10, IsInaccessible = false, SyncStatus = SyncStatus.InSync },
                new MockWorkspace { Id = 20, IsInaccessible = false, SyncStatus = SyncStatus.InSync },
                new MockWorkspace { Id = 30, IsInaccessible = false, SyncStatus = SyncStatus.InSync },
                new MockWorkspace { Id = 40, IsInaccessible = false, SyncStatus = SyncStatus.InSync }
            };

            if (numberOfInaccessibleWorkspaces != 0)
            {
                foreach (var index in Enumerable.Range(1, numberOfInaccessibleWorkspaces))
                {
                    var w = new MockWorkspace { Id = index, IsInaccessible = true, SyncStatus = syncStatus };
                    workspaces.Add(w);
                }
            }

            setupDataSource(workspaces);
            var transition = await state.Start().SingleAsync();

            if (numberOfInaccessibleWorkspaces == 0)
                transition.Result.Should().Be(state.NoInaccessibleWorkspaceFound);
            else
                transition.Result.Should().Be(state.FoundInaccessibleWorkspaces);
        }

        private void setupDataSource(List<MockWorkspace> workspaces)
        {
            dataSource.Workspaces.GetAll(Arg.Any<Func<IDatabaseWorkspace, bool>>(), Arg.Is(true))
                .Returns(callInfo =>
                {
                    var predicate = callInfo[0] as Func<IDatabaseWorkspace, bool>;
                    var filteredWorkspace = workspaces.Where(predicate);
                    return Observable.Return(filteredWorkspace.Cast<IThreadSafeWorkspace>());
                });
        }
    }
}
