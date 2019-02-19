using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NSubstitute.Exceptions;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Sync.States;
using Toggl.Foundation.Sync.States.Pull;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States.Pull
{
    public sealed class DeleteInaccessibleRunningTimeEntryStateTests
    {
        private readonly DeleteInaccessibleRunningTimeEntryState state;

        private readonly IDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry> dataSource =
            Substitute.For<IDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry>>();

        public DeleteInaccessibleRunningTimeEntryStateTests()
        {
            state = new DeleteInaccessibleRunningTimeEntryState(dataSource);
        }

        [Theory, LogIfTooSlow]
        [ConstructorData]
        public void ThrowsIfAnyOfTheArgumentsIsNull(bool useDataSource)
        {
            var theDataSource = useDataSource ? dataSource : null;

            Action tryingToConstructWithEmptyParameters = () =>
                new DeleteInaccessibleRunningTimeEntryState(theDataSource);

            tryingToConstructWithEmptyParameters.Should().Throw<Exception>();
        }

        [Fact, LogIfTooSlow]
        public async Task DeletesAnyRunningTimeEntryInSyncInInaccessibleWorkspaces()
        {
            var fetch = Substitute.For<IFetchObservables>();

            var start = new DateTimeOffset(2018, 10, 23, 15, 42, 00, TimeSpan.Zero);
            var duration = (long)TimeSpan.FromMinutes(15).TotalSeconds;

            var workspace = new MockWorkspace(1, isInaccessible: true);
            var runningTimeEntry = new MockTimeEntry(11, workspace, start: start, syncStatus: SyncStatus.InSync);
            var timeEntry1 = new MockTimeEntry(11, workspace, start: start, duration: duration);
            var timeEntry2 = new MockTimeEntry(11, workspace, start: start, duration: duration);

            configureDataSource(new[] { runningTimeEntry, timeEntry1, timeEntry2 });

            await state.Start(fetch).SingleAsync();

            await dataSource.Received().Delete(Arg.Is<long>(id => runningTimeEntry.Id == id));
        }

        [Theory, LogIfTooSlow]
        [InlineData(SyncStatus.SyncNeeded)]
        [InlineData(SyncStatus.SyncFailed)]
        [InlineData(SyncStatus.RefetchingNeeded)]
        public async Task DoesNotDeleteAnyRunningTimeEntryNotInSyncInInaccessibleWorkspaces(SyncStatus syncStatus)
        {
            var fetch = Substitute.For<IFetchObservables>();

            var start = new DateTimeOffset(2018, 10, 23, 15, 42, 00, TimeSpan.Zero);
            var duration = (long)TimeSpan.FromMinutes(15).TotalSeconds;

            var workspace = new MockWorkspace(1, isInaccessible: true);
            var runningTimeEntry = new MockTimeEntry(11, workspace, start: start, syncStatus: syncStatus);
            var timeEntry1 = new MockTimeEntry(11, workspace, start: start, duration: duration);
            var timeEntry2 = new MockTimeEntry(11, workspace, start: start, duration: duration);

            configureDataSource(new[] { runningTimeEntry, timeEntry1, timeEntry2 });

            await state.Start(fetch).SingleAsync();

            await dataSource.DidNotReceive().Delete(Arg.Any<long>());
        }

        [Theory, LogIfTooSlow]
        [InlineData(SyncStatus.InSync)]
        [InlineData(SyncStatus.SyncNeeded)]
        [InlineData(SyncStatus.SyncFailed)]
        [InlineData(SyncStatus.RefetchingNeeded)]
        public async Task DoesNotDeleteAnyRunningTimeEntryInAccessibleWorkspaces(SyncStatus syncStatus)
        {
            var fetch = Substitute.For<IFetchObservables>();

            var start = new DateTimeOffset(2018, 10, 23, 15, 42, 00, TimeSpan.Zero);
            var duration = (long)TimeSpan.FromMinutes(15).TotalSeconds;

            var workspace = new MockWorkspace(1, isInaccessible: false);
            var runningTimeEntry = new MockTimeEntry(11, workspace, start: start, syncStatus: syncStatus);
            var timeEntry1 = new MockTimeEntry(11, workspace, start: start, duration: duration);
            var timeEntry2 = new MockTimeEntry(11, workspace, start: start, duration: duration);

            configureDataSource(new[] { runningTimeEntry, timeEntry1, timeEntry2 });

            await state.Start(fetch).SingleAsync();

            await dataSource.DidNotReceive().Delete(Arg.Any<long>());
        }

        private void configureDataSource(IEnumerable<IThreadSafeTimeEntry> timeEntries)
        {
            dataSource
                .GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>(), Arg.Is(true))
                .Returns(callInfo =>
                {
                    var predicate = callInfo[0] as Func<IDatabaseTimeEntry, bool>;
                    var filteredTimeEntries = timeEntries.Where(predicate);
                    return Observable.Return(filteredTimeEntries.Cast<IThreadSafeTimeEntry>());
                });
        }
    }
}
