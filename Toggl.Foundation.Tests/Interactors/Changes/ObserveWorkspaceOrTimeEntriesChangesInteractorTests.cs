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

namespace Toggl.Foundation.Tests.Interactors.Changes
{
    public class ObserveWorkspaceOrTimeEntriesChangesInteractorTests : BaseInteractorTests
    {
        [Fact, LogIfTooSlow]
        public void GetsAnEventWhenAChangeToWorkspacesHappens()
        {
            var createSubject = new Subject<IThreadSafeWorkspace>();
            DataSource.Workspaces.Created.Returns(createSubject.AsObservable());
            DataSource.Workspaces.Updated.Returns(Observable.Never<EntityUpdate<IThreadSafeWorkspace>>());
            DataSource.Workspaces.Deleted.Returns(Observable.Never<long>());

            var testScheduler = new TestScheduler();
            var observer = testScheduler.CreateObserver<Unit>();

            InteractorFactory.ObserveWorkspaceOrTimeEntriesChanges().Execute()
                .Subscribe(observer);

            var mockWorkspace = new MockWorkspace { Id = 42 };
            createSubject.OnNext(mockWorkspace);
            createSubject.OnNext(mockWorkspace);

            observer.Messages.Should().HaveCount(2);
        }

        [Fact, LogIfTooSlow]
        public void GetsAnEventWhenAChangeToTimeEntriesHappens()
        {
            var createSubject = new Subject<IThreadSafeTimeEntry>();
            DataSource.TimeEntries.Created.Returns(createSubject.AsObservable());
            DataSource.TimeEntries.Updated.Returns(Observable.Never<EntityUpdate<IThreadSafeTimeEntry>>());
            DataSource.TimeEntries.Deleted.Returns(Observable.Never<long>());

            var testScheduler = new TestScheduler();
            var observer = testScheduler.CreateObserver<Unit>();

            InteractorFactory.ObserveWorkspaceOrTimeEntriesChanges().Execute()
                .Subscribe(observer);

            var mockTimeEntry = new MockTimeEntry { Id = 42 };
            createSubject.OnNext(mockTimeEntry);
            createSubject.OnNext(mockTimeEntry);

            observer.Messages.Should().HaveCount(2);
        }


        [Fact, LogIfTooSlow]
        public void GetsAnEventWhenAChangeToTimeEntriesOrWorkspacesHappens()
        {
            var timeEntryCreateSubject = new Subject<IThreadSafeTimeEntry>();
            DataSource.TimeEntries.Created.Returns(timeEntryCreateSubject.AsObservable());
            DataSource.TimeEntries.Updated.Returns(Observable.Never<EntityUpdate<IThreadSafeTimeEntry>>());
            DataSource.TimeEntries.Deleted.Returns(Observable.Never<long>());

            var workspaceCreateSubject = new Subject<IThreadSafeWorkspace>();
            DataSource.Workspaces.Created.Returns(workspaceCreateSubject.AsObservable());
            DataSource.Workspaces.Updated.Returns(Observable.Never<EntityUpdate<IThreadSafeWorkspace>>());
            DataSource.Workspaces.Deleted.Returns(Observable.Never<long>());

            var testScheduler = new TestScheduler();
            var observer = testScheduler.CreateObserver<Unit>();

            InteractorFactory.ObserveWorkspaceOrTimeEntriesChanges().Execute()
                .Subscribe(observer);

            timeEntryCreateSubject.OnNext(new MockTimeEntry { Id = 42 });
            workspaceCreateSubject.OnNext(new MockWorkspace { Id = 42 });
            timeEntryCreateSubject.OnNext(new MockTimeEntry { Id = 43 });

            observer.Messages.Should().HaveCount(2);
        }
    }
}
