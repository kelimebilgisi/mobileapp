using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Tests.Mocks;
using Xunit;

namespace Toggl.Foundation.Tests.Interactors.Workspace
{
    public class ObserveWorkspacesChangesInteractorTests
    {
        public sealed class TheObserveAllWorkspacesInteractorTests : BaseInteractorTests
        {
            [Fact, LogIfTooSlow]
            public void GetsAnEventWhenAChangeToWorkspacesHappens()
            {
                var createSubject = new Subject<IThreadSafeWorkspace>();
                var deleteSubject = new Subject<long>();
                DataSource.Workspaces.Created.Returns(createSubject.AsObservable());
                DataSource.Workspaces.Updated.Returns(Observable.Never<EntityUpdate<IThreadSafeWorkspace>>());
                DataSource.Workspaces.Deleted.Returns(deleteSubject.AsObservable());

                var testScheduler = new TestScheduler();
                var observer = testScheduler.CreateObserver<Unit>();

                InteractorFactory.ObserveWorkspacesChanges().Execute()
                    .Subscribe(observer);

                var mockWorkspace = new MockWorkspace { Id = 42 };
                createSubject.OnNext(mockWorkspace);
                deleteSubject.OnNext(3);

                observer.Messages.Should().HaveCount(2);
            }
        }
    }
}
