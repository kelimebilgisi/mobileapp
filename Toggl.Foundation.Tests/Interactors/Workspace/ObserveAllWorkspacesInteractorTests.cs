using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Foundation.Tests.TestExtensions;
using Xunit;

namespace Toggl.Foundation.Tests.Interactors.Workspace
{
    public class ObserveAllWorkspacesInteractorTests
    {
        public sealed class TheObserveAllWorkspacesInteractor : BaseInteractorTests
        {
            [Fact, LogIfTooSlow]
            public async Task GetsAllChangesToWorkspaces()
            {
                var createSubject = new Subject<IThreadSafeWorkspace>();
                DataSource.Workspaces.Created.Returns(createSubject.AsObservable());
                DataSource.Workspaces.Updated.Returns(Observable.Never<EntityUpdate<IThreadSafeWorkspace>>());
                DataSource.Workspaces.Deleted.Returns(Observable.Never<long>());

                var workspaces = Enumerable.Range(0, 10)
                    .Select(id => new MockWorkspace { Id = id });
                DataSource.Workspaces.GetAll().Returns(Observable.Return(workspaces));

                var testScheduler = new TestScheduler();
                var observer = testScheduler.CreateObserver<IEnumerable<IThreadSafeWorkspace>>();

                InteractorFactory.ObserveAllWorkspaces().Execute()
                    .Subscribe(observer);

                var mockWorkspace = new MockWorkspace { Id = 42 };
                var newWorkspaces = workspaces.Append(mockWorkspace);
                DataSource.Workspaces.GetAll().Returns(Observable.Return(newWorkspaces));
                createSubject.OnNext(mockWorkspace);

                observer.Messages.Should().HaveCount(2);
                observer.Messages.First().Value.Value.Should().BeEquivalentTo(workspaces);
                observer.LastEmittedValue().Should().BeEquivalentTo(newWorkspaces);
            }

            [Fact, LogIfTooSlow]
            public async Task DoesntEmitIfWorkspacesDidntChange()
            {
                var createSubject = new Subject<IThreadSafeWorkspace>();
                DataSource.Workspaces.Created.Returns(createSubject.AsObservable());
                DataSource.Workspaces.Updated.Returns(Observable.Never<EntityUpdate<IThreadSafeWorkspace>>());
                DataSource.Workspaces.Deleted.Returns(Observable.Never<long>());

                var workspaces = Enumerable.Range(0, 10)
                    .Select(id => new MockWorkspace { Id = id });
                DataSource.Workspaces.GetAll().Returns(Observable.Return(workspaces));

                var testScheduler = new TestScheduler();
                var observer = testScheduler.CreateObserver<IEnumerable<IThreadSafeWorkspace>>();

                InteractorFactory.ObserveAllWorkspaces().Execute()
                    .Subscribe(observer);

                createSubject.OnNext(new MockWorkspace { Id = 42 });

                observer.Messages.Should().HaveCount(1);
                observer.Messages.First().Value.Value.Should().BeEquivalentTo(workspaces);
            }
        }
    }
}
