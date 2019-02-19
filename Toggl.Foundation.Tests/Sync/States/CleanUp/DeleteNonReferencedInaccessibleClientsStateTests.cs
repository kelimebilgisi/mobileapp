using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NSubstitute;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Sync.States.CleanUp;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Models;
using Xunit;

namespace Toggl.Foundation.Tests.Sync.States.CleanUp
{
    public sealed class DeleteNonReferencedInaccessibleClientsStateTests
    {
        private readonly DeleteNonReferencedInaccessibleClientsState state;

        private readonly IDataSource<IThreadSafeClient, IDatabaseClient> clientsDataSource =
            Substitute.For<IDataSource<IThreadSafeClient, IDatabaseClient>>();

        private readonly IDataSource<IThreadSafeProject, IDatabaseProject> projectsDataSource =
            Substitute.For<IDataSource<IThreadSafeProject, IDatabaseProject>>();

        public DeleteNonReferencedInaccessibleClientsStateTests()
        {
            state = new DeleteNonReferencedInaccessibleClientsState(clientsDataSource, projectsDataSource);
        }

        [Fact, LogIfTooSlow]
        public async Task DeletesUnreferencedClientsInInaccessibleWorkspaces()
        {
            var accessibleWorkspace = new MockWorkspace(1000, isInaccessible: false);
            var inaccessibleWorkspace = new MockWorkspace(2000, isInaccessible: true);

            var client1 = new MockClient(1001, accessibleWorkspace, SyncStatus.InSync);
            var client2 = new MockClient(1002, accessibleWorkspace, SyncStatus.SyncNeeded);
            var client3 = new MockClient(2001, inaccessibleWorkspace, SyncStatus.SyncNeeded);
            var client4 = new MockClient(2002, inaccessibleWorkspace, SyncStatus.InSync);

            var project1 = new MockProject(101, accessibleWorkspace, client1, syncStatus: SyncStatus.InSync);
            var project2 = new MockProject(102, accessibleWorkspace, client2, syncStatus: SyncStatus.InSync);
            var project3 = new MockProject(201, inaccessibleWorkspace, client3, syncStatus: SyncStatus.InSync);

            var clients = new[] { client1, client2, client3, client4 };
            var projects = new[] { project1, project2, project3 };

            var unreferencedClients = new[] { client4 };
            var neededClients = clients.Where(client => !unreferencedClients.Contains(client));

            configureDataSource(clients, projects);

            await state.Start().SingleAsync();

            await clientsDataSource.Received().DeleteAll(Arg.Is<IEnumerable<IThreadSafeClient>>(arg =>
                arg.All(client => unreferencedClients.Contains(client)) &&
                arg.None(client => neededClients.Contains(client))));
        }

        private void configureDataSource(IThreadSafeClient[] clients, IThreadSafeProject[] projects)
        {
            clientsDataSource
                .GetAll(Arg.Any<Func<IDatabaseClient, bool>>(), Arg.Is(true))
                .Returns(callInfo =>
                {
                    var predicate = callInfo[0] as Func<IDatabaseClient, bool>;
                    var filteredClients = clients.Where(predicate);
                    return Observable.Return(filteredClients.Cast<IThreadSafeClient>());
                });

            projectsDataSource
                .GetAll(Arg.Any<Func<IDatabaseProject, bool>>(), Arg.Is(true))
                .Returns(callInfo =>
                {
                    var predicate = callInfo[0] as Func<IDatabaseProject, bool>;
                    var filteredProjects = projects.Where(predicate);
                    return Observable.Return(filteredProjects.Cast<IThreadSafeProject>());
                });
        }
    }
}
