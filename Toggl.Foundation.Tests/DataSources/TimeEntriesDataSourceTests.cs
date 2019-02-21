using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using NSubstitute.Routing.Handlers;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.DTOs;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Foundation.Tests.TestExtensions;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Exceptions;
using Toggl.PrimeRadiant.Models;
using Xunit;
using ThreadingTask = System.Threading.Tasks.Task;

namespace Toggl.Foundation.Tests.DataSources
{
    public sealed class TimeEntriesDataSourceTests
    {
        public abstract class TimeEntryDataSourceTest : BaseInteractorTests
        {
            protected const long ProjectId = 10;

            protected const long WorkspaceId = 11;

            protected const long UserId = 12;

            protected const long CurrentRunningId = 13;

            protected const long TaskId = 14;

            protected ITimeEntriesSource TimeEntriesSource { get; }

            protected TestScheduler TestScheduler { get; } = new TestScheduler();

            protected static DateTimeOffset Now { get; } = new DateTimeOffset(2018, 05, 14, 18, 00, 00, TimeSpan.Zero);

            protected IThreadSafeTimeEntry TimeEntry { get; } =
                Models.TimeEntry.Builder
                    .Create(CurrentRunningId)
                    .SetUserId(UserId)
                    .SetDescription("")
                    .SetWorkspaceId(WorkspaceId)
                    .SetSyncStatus(SyncStatus.InSync)
                    .SetAt(Now.AddDays(-1))
                    .SetStart(Now.AddHours(-2))
                    .Build();

            protected IRepository<IDatabaseTimeEntry> Repository { get; } = Substitute.For<IRepository<IDatabaseTimeEntry>>();

            protected TimeEntryDataSourceTest()
            {
                TimeEntriesSource = new TimeEntriesDataSource(Repository, TimeService, AnalyticsService);

                IdProvider.GetNextIdentifier().Returns(-1);
                Repository.GetById(Arg.Is(TimeEntry.Id)).Returns(Observable.Return(TimeEntry));

                Repository.Create(Arg.Any<IDatabaseTimeEntry>())
                          .Returns(info => Observable.Return(info.Arg<IDatabaseTimeEntry>()));

                Repository.Update(Arg.Any<long>(), Arg.Any<IDatabaseTimeEntry>())
                          .Returns(info => Observable.Return(info.Arg<IDatabaseTimeEntry>()));

                TimeService.CurrentDateTime.Returns(Now);
            }
        }

        public sealed class TheConstructor : TimeEntryDataSourceTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useRepository,
                bool useTimeService,
                bool useAnalyticsService)
            {
                var repository = useRepository ? Repository : null;
                var timeService = useTimeService ? TimeService : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new TimeEntriesDataSource(repository, timeService, analyticsService);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }

            [Fact]
            public void FixesMultipleRunningTimeEntriesDatabaseInconsistency()
            {
                Repository.GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                    .Returns(
                        Observable.Return(new[]
                        {
                            new MockTimeEntry { Id = 1, Duration = null, IsDeleted = false },
                            new MockTimeEntry { Id = 2, Duration = null, IsDeleted = false },
                        }),
                        Observable.Return(new[]
                        {
                            new MockTimeEntry { Id = 1, Duration = null, IsDeleted = false }
                        }));

                // ReSharper disable once ObjectCreationAsStatement
                new TimeEntriesDataSource(Repository, TimeService, AnalyticsService);

                Repository.Received().BatchUpdate(
                    Arg.Is<IEnumerable<(long Id, IDatabaseTimeEntry Entity)>>(
                        timeEntries => timeEntries.Count() == 2
                                       && timeEntries.Any(tuple => tuple.Id == 1 && tuple.Entity.Duration == null)
                                       && timeEntries.Any(tuple => tuple.Id == 2 && tuple.Entity.Duration == null)),
                    Arg.Any<Func<IDatabaseTimeEntry, IDatabaseTimeEntry, ConflictResolutionMode>>(),
                    Arg.Is<IRivalsResolver<IDatabaseTimeEntry>>(rivalsResolver => rivalsResolver != null));
                AnalyticsService.TwoRunningTimeEntriesInconsistencyFixed.Received().Track();
            }

            [Fact]
            public void DoesNotTrackTheEventIfThereAreNotTwoRunningTimeEntries()
            {
                Repository.GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                    .Returns(
                        Observable.Return(new[]
                        {
                            new MockTimeEntry { Id = 1, Duration = null, IsDeleted = false }
                        }));

                // ReSharper disable once ObjectCreationAsStatement
                new TimeEntriesDataSource(Repository, TimeService, AnalyticsService);

                AnalyticsService.TwoRunningTimeEntriesInconsistencyFixed.DidNotReceive().Track();
            }
        }

        public sealed class TheCreateMethod : TimeEntryDataSourceTest
        {
            [Fact]
            public async ThreadingTask CallsRepositoryWithConflictResolvers()
            {
                var timeEntry = new MockTimeEntry();
                Repository.BatchUpdate(null, null, null)
                    .ReturnsForAnyArgs(Observable.Return(new[] { new CreateResult<IDatabaseTimeEntry>(timeEntry) }));

                await TimeEntriesSource.Create(timeEntry);

                await Repository.Received().BatchUpdate(
                    Arg.Any<IEnumerable<(long, IDatabaseTimeEntry)>>(),
                    Arg.Is<Func<IDatabaseTimeEntry, IDatabaseTimeEntry, ConflictResolutionMode>>(conflictResolution => conflictResolution != null),
                    Arg.Is<IRivalsResolver<IDatabaseTimeEntry>>(resolver => resolver != null));
            }

            [Fact]
            public async ThreadingTask EmitsObservableEventsForTheNewlyCreatedRunningTimeEntry()
            {
                var createdObserver = TestScheduler.CreateObserver<IThreadSafeTimeEntry>();
                var newTimeEntry = new MockTimeEntry { Id = -1, Duration = null };
                Repository.BatchUpdate(
                    Arg.Any<IEnumerable<(long, IDatabaseTimeEntry)>>(),
                    Arg.Any<Func<IDatabaseTimeEntry, IDatabaseTimeEntry, ConflictResolutionMode>>(),
                    Arg.Any<IRivalsResolver<IDatabaseTimeEntry>>())
                    .Returns(Observable.Return(new IConflictResolutionResult<IDatabaseTimeEntry>[]
                    {
                        new CreateResult<IDatabaseTimeEntry>(newTimeEntry)
                    }));

                var timeEntriesSource = new TimeEntriesDataSource(Repository, TimeService, AnalyticsService);
                timeEntriesSource.Created.Subscribe(createdObserver);
                await timeEntriesSource.Create(newTimeEntry);

                createdObserver.SingleEmittedValue().Id.Should().Be(newTimeEntry.Id);
                createdObserver.SingleEmittedValue().Duration.Should().BeNull();
            }

            [Fact]
            public async ThreadingTask EmitsObservableEventsForTheNewRunningTimeEntryAndTheStoppedTimeEntry()
            {
                var durationAfterStopping = 100;
                var updatedObserver = TestScheduler.CreateObserver<EntityUpdate<IThreadSafeTimeEntry>>();
                var createdObserver = TestScheduler.CreateObserver<IThreadSafeTimeEntry>();
                var runningTimeEntry = new MockTimeEntry { Id = 1, Duration = null };
                var newTimeEntry = new MockTimeEntry { Id = -2, Duration = null };
                Repository.GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>())
                    .Returns(Observable.Return(new IDatabaseTimeEntry[] { runningTimeEntry }));
                Repository.BatchUpdate(
                    Arg.Any<IEnumerable<(long, IDatabaseTimeEntry)>>(),
                    Arg.Any<Func<IDatabaseTimeEntry, IDatabaseTimeEntry, ConflictResolutionMode>>(),
                    Arg.Any<IRivalsResolver<IDatabaseTimeEntry>>())
                    .Returns(Observable.Return(new IConflictResolutionResult<IDatabaseTimeEntry>[]
                    {
                        new UpdateResult<IDatabaseTimeEntry>(runningTimeEntry.Id, runningTimeEntry.With(durationAfterStopping)),
                        new CreateResult<IDatabaseTimeEntry>(newTimeEntry)
                    }));

                var timeEntriesSource = new TimeEntriesDataSource(Repository, TimeService, AnalyticsService);
                timeEntriesSource.Updated.Subscribe(updatedObserver);
                timeEntriesSource.Created.Subscribe(createdObserver);
                await timeEntriesSource.Create(newTimeEntry);

                updatedObserver.SingleEmittedValue().Entity.Duration.Should().Be(durationAfterStopping);
                createdObserver.SingleEmittedValue().Id.Should().Be(newTimeEntry.Id);
                createdObserver.SingleEmittedValue().Duration.Should().BeNull();
            }
        }

        public sealed class TheGetAllMethod : TimeEntryDataSourceTest
        {
            [Fact, LogIfTooSlow]
            public async ThreadingTask NeverReturnsDeletedTimeEntries()
            {
                var result = Enumerable
                    .Range(0, 20)
                    .Select(i =>
                    {
                        var isDeleted = i % 2 == 0;
                        var timeEntry = Substitute.For<IThreadSafeTimeEntry>();
                        timeEntry.Id.Returns(i);
                        timeEntry.IsDeleted.Returns(isDeleted);
                        return timeEntry;
                    });
                DataSource
                    .TimeEntries
                    .GetAll(Arg.Any<Func<IDatabaseTimeEntry, bool>>(), true)
                    .Returns(callInfo =>
                        Observable
                             .Return(result)
                             .Select(x => x.Where(callInfo.Arg<Func<IThreadSafeTimeEntry, bool>>())));

                var timeEntries = await InteractorFactory.GetAllTimeEntriesVisibleToTheUser().Execute()
                    .Select(tes => tes.Where(x => x.Id > 10));

                timeEntries.Should().HaveCount(5);
            }
        }
    }
}
