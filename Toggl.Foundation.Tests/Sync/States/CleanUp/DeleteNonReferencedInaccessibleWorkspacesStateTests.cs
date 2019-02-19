using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Sync.States.CleanUp;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States.CleanUp
{
    public class DeleteNonReferencedInaccessibleWorkspacesStateTests
    {
        private readonly DeleteNonReferencedInaccessibleWorkspacesState state;

        private readonly IDataSource<IThreadSafeWorkspace, IDatabaseWorkspace> workspacesDataSource =
            Substitute.For<IDataSource<IThreadSafeWorkspace, IDatabaseWorkspace>>();

        private readonly IDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry> timeEntriesDataSource =
            Substitute.For<IDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry>>();

        private readonly IDataSource<IThreadSafeProject, IDatabaseProject> projectsDataSource =
            Substitute.For<IDataSource<IThreadSafeProject, IDatabaseProject>>();

        private readonly IDataSource<IThreadSafeTask, IDatabaseTask> tasksDataSource =
            Substitute.For<IDataSource<IThreadSafeTask, IDatabaseTask>>();

        private readonly IDataSource<IThreadSafeClient, IDatabaseClient> clientsDataSource =
            Substitute.For<IDataSource<IThreadSafeClient, IDatabaseClient>>();

        private readonly IDataSource<IThreadSafeTag, IDatabaseTag> tagsDataSource =
            Substitute.For<IDataSource<IThreadSafeTag, IDatabaseTag>>();

        public DeleteNonReferencedInaccessibleWorkspacesStateTests()
        {
            state = new DeleteNonReferencedInaccessibleWorkspacesState(
                workspacesDataSource,
                timeEntriesDataSource,
                projectsDataSource,
                tasksDataSource,
                clientsDataSource,
                tagsDataSource
            );
        }

        [Theory, LogIfTooSlow]
        [ConstructorData]
        public void ThrowsIfAnyOfTheArgumentsIsNull(
            bool useWorkspaces,
            bool useTimeEntries,
            bool useProjects,
            bool useTasks,
            bool useClients,
            bool useTags)
        {
            var theWorkspaces = useWorkspaces ? workspacesDataSource : null;
            var theTimeEntries = useTimeEntries ? timeEntriesDataSource : null;
            var theProjects = useProjects ? projectsDataSource : null;
            var theTasks = useTasks ? tasksDataSource : null;
            var theClients = useClients ? clientsDataSource : null;
            var theTags = useTags ? tagsDataSource : null;

            Action tryingToConstructWithEmptyParameters = () =>
                new DeleteNonReferencedInaccessibleWorkspacesState(
                    theWorkspaces,
                    theTimeEntries,
                    theProjects,
                    theTasks,
                    theClients,
                    theTags
                );

            tryingToConstructWithEmptyParameters.Should().Throw<Exception>();
        }

        [Fact, LogIfTooSlow]
        public async Task DeleteInaccessibleWorkspacesNotReferencedByTimeEntries()
        {
            var accessibleReferenced = new MockWorkspace(1, false, SyncStatus.InSync);
            var accessibleUnreferenced = new MockWorkspace(2, false, SyncStatus.InSync);
            var inaccessibleReferenced = new MockWorkspace(3, true, SyncStatus.InSync);
            var inaccessibleUnreferenced = new MockWorkspace(4, true, SyncStatus.InSync);

            var timeEntry1 = new MockTimeEntry(11, accessibleReferenced, syncStatus: SyncStatus.InSync);
            var timeEntry2 = new MockTimeEntry(22, inaccessibleReferenced, syncStatus: SyncStatus.InSync);

            var workspaces = new[] { accessibleReferenced, accessibleUnreferenced, inaccessibleReferenced, inaccessibleUnreferenced };
            var timeEntries = new[] { timeEntry1, timeEntry2 };

            configureDataSources(workspaces, timeEntries: timeEntries);

            await state.Start().SingleAsync();

            await workspacesDataSource.Received().DeleteAll(Arg.Is<IEnumerable<IThreadSafeWorkspace>>(
                arg => arg.All(ws => ws == inaccessibleUnreferenced)));
        }

        [Fact, LogIfTooSlow]
        public async Task DeleteInaccessibleWorkspacesNotReferencedByProjects()
        {
            var accessibleReferenced = new MockWorkspace(1, false, SyncStatus.InSync);
            var accessibleUnreferenced = new MockWorkspace(2, false, SyncStatus.InSync);
            var inaccessibleReferenced = new MockWorkspace(3, true, SyncStatus.InSync);
            var inaccessibleUnreferenced = new MockWorkspace(4, true, SyncStatus.InSync);

            var project1 = new MockProject(11, accessibleReferenced, syncStatus: SyncStatus.InSync);
            var project2 = new MockProject(22, inaccessibleReferenced, syncStatus: SyncStatus.InSync);

            var workspaces = new[] { accessibleReferenced, accessibleUnreferenced, inaccessibleReferenced, inaccessibleUnreferenced };
            var projects = new[] { project1, project2 };

            configureDataSources(workspaces, projects: projects);

            await state.Start().SingleAsync();

            await workspacesDataSource.Received().DeleteAll(Arg.Is<IEnumerable<IThreadSafeWorkspace>>(
                arg => arg.All(ws => ws == inaccessibleUnreferenced)));
        }

        [Fact, LogIfTooSlow]
        public async Task DeleteInaccessibleWorkspacesNotReferencedByTasks()
        {
            var accessibleReferenced = new MockWorkspace(1, false, SyncStatus.InSync);
            var accessibleUnreferenced = new MockWorkspace(2, false, SyncStatus.InSync);
            var inaccessibleReferenced = new MockWorkspace(3, true, SyncStatus.InSync);
            var inaccessibleUnreferenced = new MockWorkspace(4, true, SyncStatus.InSync);

            var project1 = new MockProject(11, accessibleReferenced, syncStatus: SyncStatus.InSync);
            var project2 = new MockProject(22, inaccessibleReferenced, syncStatus: SyncStatus.InSync);

            var task1 = new MockTask(11, accessibleReferenced, project1, syncStatus: SyncStatus.InSync);
            var task2 = new MockTask(22, inaccessibleReferenced, project2, syncStatus: SyncStatus.InSync);

            var workspaces = new[] { accessibleReferenced, accessibleUnreferenced, inaccessibleReferenced, inaccessibleUnreferenced };
            var tasks = new[] { task1, task2 };

            configureDataSources(workspaces, tasks: tasks);

            await state.Start().SingleAsync();

            await workspacesDataSource.Received().DeleteAll(Arg.Is<IEnumerable<IThreadSafeWorkspace>>(
                arg => arg.All(ws => ws == inaccessibleUnreferenced)));
        }

        [Fact, LogIfTooSlow]
        public async Task DeleteInaccessibleWorkspacesNotReferencedByClients()
        {
            var accessibleReferenced = new MockWorkspace(1, false, SyncStatus.InSync);
            var accessibleUnreferenced = new MockWorkspace(2, false, SyncStatus.InSync);
            var inaccessibleReferenced = new MockWorkspace(3, true, SyncStatus.InSync);
            var inaccessibleUnreferenced = new MockWorkspace(4, true, SyncStatus.InSync);

            var client1 = new MockClient(11, accessibleReferenced, syncStatus: SyncStatus.InSync);
            var client2 = new MockClient(22, inaccessibleReferenced, syncStatus: SyncStatus.InSync);

            var workspaces = new[] { accessibleReferenced, accessibleUnreferenced, inaccessibleReferenced, inaccessibleUnreferenced };
            var clients = new[] { client1, client2 };

            configureDataSources(workspaces, clients: clients);

            await state.Start().SingleAsync();

            await workspacesDataSource.Received().DeleteAll(Arg.Is<IEnumerable<IThreadSafeWorkspace>>(
                arg => arg.All(ws => ws == inaccessibleUnreferenced)));
        }

        [Fact, LogIfTooSlow]
        public async Task DeleteInaccessibleWorkspacesNotReferencedByTags()
        {
            var accessibleReferenced = new MockWorkspace(1, false, SyncStatus.InSync);
            var accessibleUnreferenced = new MockWorkspace(2, false, SyncStatus.InSync);
            var inaccessibleReferenced = new MockWorkspace(3, true, SyncStatus.InSync);
            var inaccessibleUnreferenced = new MockWorkspace(4, true, SyncStatus.InSync);

            var tag1 = new MockTag(11, accessibleReferenced, syncStatus: SyncStatus.InSync);
            var tag2 = new MockTag(22, inaccessibleReferenced, syncStatus: SyncStatus.InSync);

            var workspaces = new[] { accessibleReferenced, accessibleUnreferenced, inaccessibleReferenced, inaccessibleUnreferenced };
            var tags = new[] { tag1, tag2 };

            configureDataSources(workspaces, tags: tags);

            await state.Start().SingleAsync();

            await workspacesDataSource.Received().DeleteAll(Arg.Is<IEnumerable<IThreadSafeWorkspace>>(
                arg => arg.All(ws => ws == inaccessibleUnreferenced)));
        }

        private void configureDataSources(
            IEnumerable<IThreadSafeWorkspace> workspaces,
            IEnumerable<IThreadSafeTimeEntry> timeEntries = null,
            IEnumerable<IThreadSafeProject> projects = null,
            IEnumerable<IThreadSafeTask> tasks = null,
            IEnumerable<IThreadSafeClient> clients = null,
            IEnumerable<IThreadSafeTag> tags = null)
        {
            configureDataSource(workspacesDataSource, workspaces);
            configureDataSource(timeEntriesDataSource, timeEntries ?? Array.Empty<IThreadSafeTimeEntry>());
            configureDataSource(projectsDataSource, projects ?? Array.Empty<IThreadSafeProject>());
            configureDataSource(tasksDataSource, tasks ?? Array.Empty<IThreadSafeTask>());
            configureDataSource(clientsDataSource, clients ?? Array.Empty<IThreadSafeClient>());
            configureDataSource(tagsDataSource, tags ?? Array.Empty<IThreadSafeTag>());
        }

        private void configureDataSource<TInterface, TDatabaseInterface>(
            IDataSource<TInterface, TDatabaseInterface> dataSource,
            IEnumerable<TInterface> collection)
            where TInterface : TDatabaseInterface, IThreadSafeModel
            where TDatabaseInterface : IDatabaseModel
        {
            dataSource
                .GetAll(Arg.Any<Func<TDatabaseInterface, bool>>(), Arg.Is(true))
                .Returns(callInfo =>
                {
                    var predicate = callInfo[0] as Func<TDatabaseInterface, bool>;
                    var filteredCollection = collection.Cast<TDatabaseInterface>().Where(predicate);
                    return Observable.Return(filteredCollection.Cast<TInterface>());
                });
        }
    }
}
