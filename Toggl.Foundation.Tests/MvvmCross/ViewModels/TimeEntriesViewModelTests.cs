using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using MvvmCross.Navigation;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Helper;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.TimeEntriesLog;
using Toggl.Foundation.Services;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Xunit;
using ThreadingTask = System.Threading.Tasks.Task;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    using TimeEntriesLog = IEnumerable<ISectionModel<DaySummaryViewModel, LogItemViewModel>>;

    public sealed class TimeEntriesViewModelTests
    {
        public abstract class TimeEntriesViewModelTest
        {
            protected ITogglDataSource DataSource { get; } = Substitute.For<ITogglDataSource>();
            protected IInteractorFactory InteractorFactory { get; } = Substitute.For<IInteractorFactory>();
            protected IAnalyticsService AnalyticsService { get; } = Substitute.For<IAnalyticsService>();
            protected TestSchedulerProvider SchedulerProvider { get; } = new TestSchedulerProvider();
            protected IRxActionFactory RxActionFactory { get; }
            protected ITimeService TimeService { get; } = Substitute.For<ITimeService>();

            protected TimeEntriesViewModel ViewModel { get; private set; }

            protected TimeEntriesViewModel CreateViewModel()
                => new TimeEntriesViewModel(DataSource, InteractorFactory, AnalyticsService, SchedulerProvider, RxActionFactory, TimeService);

            protected TimeEntriesViewModelTest()
            {
                RxActionFactory = new RxActionFactory(SchedulerProvider);
                ViewModel = CreateViewModel();
            }
        }

        public sealed class TheConstructor : TimeEntriesViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useDataSource,
                bool useInteractorFactory,
                bool useAnalyticsService,
                bool useSchedulerProvider,
                bool useRxActionFactory,
                bool useTimeService)
            {
                var dataSource = useDataSource ? DataSource : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var analyticsService = useAnalyticsService ? AnalyticsService : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;
                var timeService = useTimeService ? TimeService : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new TimeEntriesViewModel(dataSource, interactorFactory, analyticsService, schedulerProvider, rxActionFactory, timeService);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public abstract class TimeEntryDataSourceObservableTest : TimeEntriesViewModelTest
        {
            private static readonly DateTimeOffset now = new DateTimeOffset(2017, 01, 19, 07, 10, 00, TimeZoneInfo.Local.GetUtcOffset(new DateTime(2017, 01, 19)));

            protected const int InitialAmountOfTimeEntries = 20;

            protected ISubject<IEnumerable<IThreadSafeTimeEntry>> TimeEntries { get; }

            protected IThreadSafeTimeEntry NewTimeEntry(long? duration = null)
            {
                return new MockTimeEntry
                {
                    Id = 21,
                    UserId = 10,
                    WorkspaceId = 12,
                    Description = "",
                    At = now,
                    Duration = duration,
                    Start = now,
                    TagIds = new long[] { 1, 2, 3 },
                    Workspace = new MockWorkspace()
                };
            }

            protected TimeEntryDataSourceObservableTest()
            {
                var startTime = now.AddHours(-2);

                var initialTimeEntries =
                    Enumerable.Range(1, InitialAmountOfTimeEntries)
                        .Select(i =>
                            new MockTimeEntry
                            {
                                Id = i,
                                Start = startTime.AddHours(i * 2),
                                Duration = (long)TimeSpan.FromHours(i * 2 + 2).TotalSeconds,
                                UserId = 11,
                                WorkspaceId = 12,
                                Description = "",
                                At = now,
                                TagIds = new long[] { 1, 2, 3 },
                                Workspace = new MockWorkspace { IsInaccessible = false }
                            }
                        );

                TimeEntries = new BehaviorSubject<IEnumerable<IThreadSafeTimeEntry>>(initialTimeEntries);
                InteractorFactory.ObserveAllTimeEntriesVisibleToTheUser().Execute().Returns(TimeEntries);
            }
        }

        public sealed class TheDelayDeleteTimeEntryAction : TimeEntriesViewModelTest
        {
            private readonly TimeEntriesViewModel viewModel;
            private readonly IObserver<int?> observer = Substitute.For<IObserver<int?>>();

            public TheDelayDeleteTimeEntryAction()
            {
                viewModel = new TimeEntriesViewModel(DataSource, InteractorFactory, AnalyticsService, SchedulerProvider, RxActionFactory, TimeService);
                viewModel.TimeEntriesPendingDeletion.Subscribe(observer);
            }

            [Property]
            public void ShowsTheUndoUI(NonEmptyArray<long> ids)
            {
                observer.ClearReceivedCalls();

                viewModel.DelayDeleteTimeEntries.Execute(ids.Get);
                SchedulerProvider.TestScheduler.Start();

                observer.Received().OnNext(ids.Get.Length);
            }

            [Property]
            public void DoesNotHideTheUndoUITooEarly(NonEmptyArray<long> ids)
            {
                observer.ClearReceivedCalls();

                viewModel.DelayDeleteTimeEntries.Execute(ids.Get);
                SchedulerProvider.TestScheduler.AdvanceBy(Constants.UndoTime.Ticks - 1);

                observer.Received().OnNext(ids.Get.Length);
                observer.DidNotReceive().OnNext(null);
            }

            [Property]
            public void HidesTheUndoUIAfterSeveralSeconds(NonEmptyArray<long> ids)
            {
                observer.ClearReceivedCalls();

                viewModel.DelayDeleteTimeEntries.Execute(ids.Get);
                SchedulerProvider.TestScheduler.Start();

                Received.InOrder(() =>
                {
                    observer.OnNext(ids.Get.Length);
                    observer.OnNext(null);
                });
            }

            [Fact]
            public void DoesNotHideTheUndoUIIfAnotherItemWasDeletedWhileWaiting()
            {
                viewModel.DelayDeleteTimeEntries.Execute(new long[] { 1 });
                SchedulerProvider.TestScheduler.AdvanceBy((long)(Constants.UndoTime.Ticks * 0.5));
                viewModel.DelayDeleteTimeEntries.Execute(new long[] { 2, 3 });
                SchedulerProvider.TestScheduler.AdvanceBy((long)(Constants.UndoTime.Ticks * 0.6));

                Received.InOrder(() =>
                {
                    observer.OnNext(1);
                    observer.OnNext(2);
                });
            }

            [Fact]
            public async ThreadingTask ImmediatelyHardDeletesTheTimeEntryWhenAnotherTimeEntryIsDeletedBeforeTheUndoTimeRunsOut()
            {
                var batchA = new long[] { 1, 2, 3 };
                var batchB = new long[] { 4, 5 };

                var observableA = viewModel.DelayDeleteTimeEntries.Execute(batchA);
                SchedulerProvider.TestScheduler.AdvanceBy(Constants.UndoTime.Ticks / 2);
                var observableB = viewModel.DelayDeleteTimeEntries.Execute(batchB);
                await observableA;

                batchA.ForEach(id =>
                {
                    InteractorFactory.Received().DeleteTimeEntry(id).Execute();
                });
            }

            [Fact]
            public async ThreadingTask ImmediatelyHardDeletesTheTimeEntryWhenCallingFinilizeDelayDeleteTimeEntryIfNeeded()
            {
                var batch = new long[] { 1, 2, 3 };

                var observableA = viewModel.DelayDeleteTimeEntries.Execute(batch);
                SchedulerProvider.TestScheduler.AdvanceBy(Constants.UndoTime.Ticks / 2);
                await viewModel.FinalizeDelayDeleteTimeEntryIfNeeded();
                SchedulerProvider.TestScheduler.AdvanceBy(Constants.UndoTime.Ticks / 4);

                batch.ForEach(id =>
                {
                    InteractorFactory.Received().DeleteTimeEntry(Arg.Is(id)).Execute();
                });
            }

            [Fact]
            public void DoesNotHardDeletesTheTimeEntryWhenNotCallingFinilizeDelayDeleteTimeEntryIfNeeded()
            {
                var batch = new long[] { 1, 2, 3 };

                var observableA = viewModel.DelayDeleteTimeEntries.Execute(batch);
                SchedulerProvider.TestScheduler.AdvanceBy(Constants.UndoTime.Ticks / 2);

                batch.ForEach(id =>
                {
                    InteractorFactory.DidNotReceive().DeleteTimeEntry(Arg.Is(id)).Execute();
                });
            }

            [Fact]
            public void ReloadsTimeEntriesLog()
            {
                var observer = SchedulerProvider.TestScheduler.CreateObserver<TimeEntriesLog>();
                viewModel.TimeEntries.Subscribe(observer);

                viewModel.DelayDeleteTimeEntries.Execute(new[] { 123L });
                SchedulerProvider.TestScheduler.Start();

                // 1 - the initial load of the data
                // 2 - undo bar shown
                // 3 - undo bar hidden
                observer.Messages.Should().HaveCount(3);
            }
        }

        public sealed class TheCancelDeleteTimeEntryAction : TimeEntriesViewModelTest
        {
            private readonly TimeEntriesViewModel viewModel;
            private readonly long[] batch = { 1, 2, 3 };
            private readonly IObserver<int?> observer = Substitute.For<IObserver<int?>>();

            public TheCancelDeleteTimeEntryAction()
            {
                viewModel = new TimeEntriesViewModel(DataSource, InteractorFactory, AnalyticsService, SchedulerProvider, RxActionFactory, TimeService);
                viewModel.TimeEntriesPendingDeletion.Subscribe(observer);
            }

            [Fact]
            public async ThreadingTask DoesNotDeleteTheTimeEntryIfTheUndoIsInitiatedBeforeTheUndoPeriodIsOver()
            {
                viewModel.DelayDeleteTimeEntries.Execute(batch);
                SchedulerProvider.TestScheduler.AdvanceBy(Constants.UndoTime.Ticks / 2);
                viewModel.CancelDeleteTimeEntry.Execute();
                SchedulerProvider.TestScheduler.Start();

                batch.ForEach(id =>
                {
                    InteractorFactory.DidNotReceive().DeleteTimeEntry(Arg.Is(id)).Execute();
                });
            }

            [Fact]
            public async ThreadingTask DeletesTheTimeEntryIfTheUndoIsInitiatedAfterTheUndoPeriodIsOver()
            {
                viewModel.DelayDeleteTimeEntries.Execute(batch);
                SchedulerProvider.TestScheduler.AdvanceBy(Constants.UndoTime.Ticks);
                viewModel.CancelDeleteTimeEntry.Execute();

                batch.ForEach(id =>
                {
                    InteractorFactory.Received().DeleteTimeEntry(Arg.Is(id)).Execute();
                });
            }

            [Fact]
            public async ThreadingTask HidesTheUndoUI()
            {
                viewModel.DelayDeleteTimeEntries.Execute(batch);
                SchedulerProvider.TestScheduler.AdvanceBy(Constants.UndoTime.Ticks / 2);
                viewModel.CancelDeleteTimeEntry.Execute();
                SchedulerProvider.TestScheduler.Start();

                observer.Received().OnNext(batch.Length);
                observer.Received().OnNext(null);
            }
        }
    }
}
