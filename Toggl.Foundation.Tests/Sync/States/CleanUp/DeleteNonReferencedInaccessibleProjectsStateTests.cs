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
    public class DeleteNonReferencedInaccessibleProjectsStateTests
    {
        private readonly DeleteNonReferencedInaccessibleProjectsState state;

        private readonly IDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry> timeEntriesDataSource =
            Substitute.For<IDataSource<IThreadSafeTimeEntry, IDatabaseTimeEntry>>();

        private readonly IDataSource<IThreadSafeTask, IDatabaseTask> tasksDataSource =
            Substitute.For<IDataSource<IThreadSafeTask, IDatabaseTask>>();

        private readonly IDataSource<IThreadSafeProject, IDatabaseProject> projectsDataSource =
            Substitute.For<IDataSource<IThreadSafeProject, IDatabaseProject>>();

        public DeleteNonReferencedInaccessibleProjectsStateTests()
        {
            state = new DeleteNonReferencedInaccessibleProjectsState(
                projectsDataSource, tasksDataSource, timeEntriesDataSource
            );
        }

        [Fact, LogIfTooSlow]
        public async Task DeleteProjectsUnreferencedByTasksInInaccessibleWorkspace()
        {
            var accessibleWorkspace = new MockWorkspace(1000, isInaccessible: false);
            var inaccessibleWorkspace = new MockWorkspace(2000, isInaccessible: true);

            var project1 = new MockProject(101, accessibleWorkspace, syncStatus: SyncStatus.InSync);
            var project2 = new MockProject(102, accessibleWorkspace, syncStatus: SyncStatus.RefetchingNeeded);
            var project3 = new MockProject(201, inaccessibleWorkspace, syncStatus: SyncStatus.InSync);
            var project4 = new MockProject(202, inaccessibleWorkspace, syncStatus: SyncStatus.SyncNeeded);
            var project5 = new MockProject(203, inaccessibleWorkspace, syncStatus: SyncStatus.SyncNeeded);
            var project6 = new MockProject(204, inaccessibleWorkspace, syncStatus: SyncStatus.InSync);
            var project7 = new MockProject(205, inaccessibleWorkspace, syncStatus: SyncStatus.InSync);

            var task1 = new MockTask(1001, accessibleWorkspace, project1, SyncStatus.InSync);
            var task2 = new MockTask(1002, accessibleWorkspace, project2, SyncStatus.RefetchingNeeded);
            var task3 = new MockTask(1003, accessibleWorkspace, project2, SyncStatus.SyncNeeded);
            var task4 = new MockTask(2001, inaccessibleWorkspace, project3, SyncStatus.InSync);
            var task5 = new MockTask(2002, inaccessibleWorkspace, project4, SyncStatus.RefetchingNeeded);
            var task6 = new MockTask(2003, inaccessibleWorkspace, project3, SyncStatus.SyncNeeded);
            var task7 = new MockTask(2004, inaccessibleWorkspace, project4, SyncStatus.InSync);
            var task8 = new MockTask(2005, inaccessibleWorkspace, project4, SyncStatus.InSync);

            var projects = new[] { project1, project2, project3, project4, project5, project6, project7 };
            var tasks = new[] { task1, task2, task3, task4, task5, task6, task7, task8 };
            var timeEntries = new IThreadSafeTimeEntry[] { };

            var unreferencedProjects = new[] { project6, project7 };
            var neededProjects = projects.Where(project => !unreferencedProjects.Contains(project));

            configureDataSource(projects, tasks, timeEntries);

            await state.Start().SingleAsync();

            await projectsDataSource.Received().DeleteAll(Arg.Is<IEnumerable<IThreadSafeProject>>(arg =>
                arg.All(project => unreferencedProjects.Contains(project)) &&
                arg.None(project => neededProjects.Contains(project))));
        }

        [Fact, LogIfTooSlow]
        public async Task DeleteProjectsUnreferencedByTimeEntriesInInaccessibleWorkspace()
        {
            var accessibleWorkspace = new MockWorkspace(1000, isInaccessible: false);
            var inaccessibleWorkspace = new MockWorkspace(2000, isInaccessible: true);

            var project1 = new MockProject(101, accessibleWorkspace, syncStatus: SyncStatus.InSync);
            var project2 = new MockProject(102, accessibleWorkspace, syncStatus: SyncStatus.RefetchingNeeded);
            var project3 = new MockProject(201, inaccessibleWorkspace, syncStatus: SyncStatus.InSync);
            var project4 = new MockProject(202, inaccessibleWorkspace, syncStatus: SyncStatus.SyncNeeded);
            var project5 = new MockProject(203, inaccessibleWorkspace, syncStatus: SyncStatus.SyncNeeded);
            var project6 = new MockProject(204, inaccessibleWorkspace, syncStatus: SyncStatus.InSync);
            var project7 = new MockProject(205, inaccessibleWorkspace, syncStatus: SyncStatus.InSync);

            var te1 = new MockTimeEntry(10001, accessibleWorkspace, project: project1, syncStatus: SyncStatus.InSync);
            var te2 = new MockTimeEntry(10002, accessibleWorkspace, project: project2, syncStatus: SyncStatus.SyncNeeded);
            var te3 = new MockTimeEntry(20001, inaccessibleWorkspace, project: project3, syncStatus: SyncStatus.InSync);
            var te4 = new MockTimeEntry(20002, inaccessibleWorkspace, project: project4, syncStatus: SyncStatus.SyncNeeded);
            var te5 = new MockTimeEntry(20003, inaccessibleWorkspace, project: project4, syncStatus: SyncStatus.SyncFailed);
            var te6 = new MockTimeEntry(20004, inaccessibleWorkspace, project: project4, syncStatus: SyncStatus.InSync);

            var projects = new[] { project1, project2, project3, project4, project5, project6, project7 };
            var tasks = new IThreadSafeTask[] { };
            var timeEntries = new[] { te1, te2, te3, te4, te5, te6 };

            var unreferencedProjects = new[] { project6, project7 };
            var neededProjects = projects.Where(project => !unreferencedProjects.Contains(project));

            configureDataSource(projects, tasks, timeEntries);

            await state.Start().SingleAsync();

            await projectsDataSource.Received().DeleteAll(Arg.Is<IEnumerable<IThreadSafeProject>>(arg =>
                arg.All(project => unreferencedProjects.Contains(project)) &&
                arg.None(project => neededProjects.Contains(project))));
        }

        [Fact, LogIfTooSlow]
        public async Task DeleteUnreferencedProjectsInInaccessibleWorkspace()
        {
            var workspace = new MockWorkspace(2000, isInaccessible: true);

            var project1 = new MockProject(101, workspace, syncStatus: SyncStatus.InSync);
            var project2 = new MockProject(102, workspace, syncStatus: SyncStatus.RefetchingNeeded);
            var project3 = new MockProject(103, workspace, syncStatus: SyncStatus.InSync);
            var project4 = new MockProject(104, workspace, syncStatus: SyncStatus.SyncNeeded);
            var project5 = new MockProject(105, workspace, syncStatus: SyncStatus.InSync);
            var project6 = new MockProject(106, workspace, syncStatus: SyncStatus.InSync);
            var project7 = new MockProject(107, workspace, syncStatus: SyncStatus.InSync);
            var project8 = new MockProject(108, workspace, syncStatus: SyncStatus.InSync);

            var task1 = new MockTask(1001, workspace, project1, SyncStatus.InSync);
            var task2 = new MockTask(1002, workspace, project2, SyncStatus.RefetchingNeeded);
            var task3 = new MockTask(1003, workspace, project3, SyncStatus.SyncNeeded);
            var task4 = new MockTask(1004, workspace, project3, SyncStatus.InSync);
            var task5 = new MockTask(1005, workspace, project6, SyncStatus.InSync);

            var te1 = new MockTimeEntry(10001, workspace, project: project1, task: task1, syncStatus: SyncStatus.InSync);
            var te2 = new MockTimeEntry(10002, workspace, project: project2, task: task2, syncStatus: SyncStatus.SyncNeeded);
            var te3 = new MockTimeEntry(20001, workspace, project: project3, task: task3, syncStatus: SyncStatus.InSync);
            var te4 = new MockTimeEntry(20002, workspace, project: project4, task: task4, syncStatus: SyncStatus.SyncNeeded);
            var te5 = new MockTimeEntry(20003, workspace, project: project4, syncStatus: SyncStatus.SyncFailed);
            var te6 = new MockTimeEntry(20004, workspace, project: project5, syncStatus: SyncStatus.InSync);

            var projects = new[] { project1, project2, project3, project4, project5, project6, project7, project8 };
            var tasks = new[] { task1, task2, task3, task4, task5 };
            var timeEntries = new[] { te1, te2, te3, te4, te5, te6 };

            var unreferencedProjects = new[] { project7, project8 };
            var neededProjects = projects.Where(project => !unreferencedProjects.Contains(project));

            configureDataSource(projects, tasks, timeEntries);

            await state.Start().SingleAsync();

            await projectsDataSource.Received().DeleteAll(Arg.Is<IEnumerable<IThreadSafeProject>>(arg =>
                arg.All(project => unreferencedProjects.Contains(project)) &&
                arg.None(project => neededProjects.Contains(project))));
        }

        private void configureDataSource(
            IThreadSafeProject[] projects,
            IThreadSafeTask[] tasks,
            IThreadSafeTimeEntry[] timeEntries
        )
        {
            projectsDataSource
                .GetAll(Arg.Any<Func<IDatabaseProject, bool>>(), Arg.Is(true))
                .Returns(callInfo =>
                {
                    var predicate = callInfo[0] as Func<IDatabaseProject, bool>;
                    var filteredProjects = projects.Where(predicate);
                    return Observable.Return(filteredProjects.Cast<IThreadSafeProject>());
                });

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
