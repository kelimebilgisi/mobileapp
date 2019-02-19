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
    public sealed class DeleteNonReferencedInaccessibleTasksStateTests
    {
        private readonly DeleteNonReferencedInaccessibleTasksState state;

        private readonly IDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry> timeEntriesDataSource =
            Substitute.For<IDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry>>();

        private readonly IDataSource<IThreadSafeTask, IDatabaseTask> tasksDataSource =
            Substitute.For<IDataSource<IThreadSafeTask, IDatabaseTask>>();

        public DeleteNonReferencedInaccessibleTasksStateTests()
        {
            state = new DeleteNonReferencedInaccessibleTasksState(tasksDataSource, timeEntriesDataSource);
        }

        [Fact, LogIfTooSlow]
        public async Task DeleteUnreferencedTasksInInaccessibleWorkspace()
        {
            var accessibleWorkspace = new MockWorkspace(1000);
            var inaccessibleWorkspace = new MockWorkspace(2000, isInaccessible: true);

            var project1 = new MockProject(101, accessibleWorkspace);
            var project2 = new MockProject(102, accessibleWorkspace, syncStatus: SyncStatus.RefetchingNeeded);
            var project3 = new MockProject(201, inaccessibleWorkspace, syncStatus: SyncStatus.SyncFailed);
            var project4 = new MockProject(202, inaccessibleWorkspace, syncStatus: SyncStatus.SyncNeeded);

            var task1 = new MockTask(1001, accessibleWorkspace, project1);
            var task2 = new MockTask(1002, accessibleWorkspace, project2, SyncStatus.RefetchingNeeded);
            var task3 = new MockTask(1003, accessibleWorkspace, project2, SyncStatus.SyncNeeded);
            var task4 = new MockTask(2001, inaccessibleWorkspace, project3);
            var task5 = new MockTask(2002, inaccessibleWorkspace, project4, SyncStatus.RefetchingNeeded);
            var task6 = new MockTask(2003, inaccessibleWorkspace, project3, SyncStatus.SyncNeeded);
            var task7 = new MockTask(2003, inaccessibleWorkspace, project4);
            var task8 = new MockTask(2004, inaccessibleWorkspace, project4);

            var te1 = new MockTimeEntry(10001, accessibleWorkspace, project: project1, task: task1);
            var te2 = new MockTimeEntry(10002, accessibleWorkspace, project: project1, task: task2);
            var te3 = new MockTimeEntry(20001, inaccessibleWorkspace, project: project3, task: task4);
            var te4 = new MockTimeEntry(20002, inaccessibleWorkspace, project: project4, task: task5);
            var te5 = new MockTimeEntry(20002, inaccessibleWorkspace, project: project4);
            var te6 = new MockTimeEntry(20002, inaccessibleWorkspace);

            var tasks = new[] { task1, task2, task3, task4, task5, task6, task7, task8 };
            var timeEntries = new[] { te1, te2, te3, te4, te5, te6 };

            var unreferencedTasks = new[] { task7, task8 };
            var neededTasks = tasks.Where(task => !unreferencedTasks.Contains(task));

            configureDataSource(tasks, timeEntries);

            await state.Start().SingleAsync();

            await tasksDataSource.Received().DeleteAll(Arg.Is<IEnumerable<IThreadSafeTask>>(arg =>
                arg.All(task => unreferencedTasks.Contains(task)) &&
                arg.None(task => neededTasks.Contains(task))));
        }

        private void configureDataSource(IThreadSafeTask[] tasks, IThreadSafeTimeEntry[] timeEntries)
        {
            tasksDataSource
                .GetAll(Arg.Any<Func<IDatabaseTask, bool>>(), Arg.Is(true))
                .Returns(callInfo =>
                {
                    var predicate = callInfo[0] as Func<IDatabaseTask, bool>;
                    var filteredTasks = tasks.Where(predicate);
                    return Observable.Return(filteredTasks.Cast<IThreadSafeTask>());
                });

            timeEntriesDataSource
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
