using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Transformations;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Multivac;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.Transformations
{
    public sealed class TimeEntriesGroupingTests
    {
        private static readonly DateTimeOffset now = new DateTimeOffset(2019, 02, 07, 16, 25, 38, TimeSpan.FromHours(-1));

        private static readonly DateTimeOffset today = now;
        private static readonly DateTimeOffset yesterday = now - TimeSpan.FromDays(1);

        private static readonly IThreadSafeWorkspace workspaceA = new MockWorkspace { Id = 1, IsInaccessible = false };
        private static readonly IThreadSafeWorkspace workspaceB = new MockWorkspace { Id = 2, IsInaccessible = false };
        private static readonly IThreadSafeProject projectA = new MockProject { Id = 1, Name = "ProjectA", Color = "blue", Active = true };
        private static readonly IThreadSafeProject projectB = new MockProject { Id = 2, Name = "ProjectB", Color = "red", Active = true };
        private static readonly IThreadSafeTask taskA = new MockTask { Id = 1, Name = "TaskA" };
        private static readonly IThreadSafeTask taskB = new MockTask { Id = 2, Name = "TaskB" };
        private static readonly IThreadSafeTag tagA = new MockTag { Id = 1, Name = "TagA" };
        private static readonly IThreadSafeTag tagB = new MockTag { Id = 2, Name = "TagB" };
        private static readonly IThreadSafeTag tagC = new MockTag { Id = 3, Name = "TagC" };

        private static readonly IThreadSafeTimeEntry[] singleDay =
        {
            createTimeEntry(now, workspaceA, "A", duration: 1),
            createTimeEntry(now, workspaceA, "A", duration: 2),
            createTimeEntry(now, workspaceA, "A", duration: 4)
        };

        private static readonly IThreadSafeTimeEntry[] twoWorkspaces =
        {
            createTimeEntry(now, workspaceA, "B", duration: 1),
            createTimeEntry(now, workspaceB, "B", duration: 2)
        };

        private static readonly IThreadSafeTimeEntry[] differentDescriptions =
        {
            createTimeEntry(now, workspaceA, "C1", duration: 1),
            createTimeEntry(now, workspaceA, "C1", duration: 2),
            createTimeEntry(now, workspaceA, "C2", duration: 4)
        };

        private static readonly IThreadSafeTimeEntry[] multipleDays =
        {
            createTimeEntry(now, workspaceA, "D1", duration: 1),
            createTimeEntry(now - TimeSpan.FromDays(1), workspaceA, "D1", duration: 2),
            createTimeEntry(now, workspaceA, "D2", duration: 4)
        };

        private static readonly IThreadSafeTimeEntry[] multipleDaysWithTags =
        {
            createTimeEntry(now, workspaceA, "E1", duration: 1),
            createTimeEntry(now - TimeSpan.FromDays(1), workspaceA, "E1", duration: 2),
            createTimeEntry(now, workspaceA, "E2", duration: 4),
            createTimeEntry(now, workspaceA, "E2", duration: 8, tags: new[] { tagA, tagB, tagC }),
            createTimeEntry(now, workspaceA, "E2", duration: 16, tags: new[] { tagA, tagC, tagB })
        };

        private static readonly IThreadSafeTimeEntry[] multipleDaysWithProject =
        {
            createTimeEntry(now, workspaceA, "F1", duration: 1),
            createTimeEntry(now - TimeSpan.FromDays(1), workspaceA, "F1", duration: 2),
            createTimeEntry(now, workspaceA, "F2", duration: 4, project: projectA),
            createTimeEntry(now, workspaceA, "F2", duration: 8),
            createTimeEntry(now, workspaceA, "F2", duration: 16, project: projectA)
        };

        private static readonly IThreadSafeTimeEntry[] differentProjects =
        {
            createTimeEntry(now, workspaceA, "G", duration: 1, project: projectA),
            createTimeEntry(now, workspaceA, "G", duration: 2, project: projectB)
        };

        private static readonly IThreadSafeTimeEntry[] sameTasks =
        {
            createTimeEntry(now, workspaceA, "H", duration: 1, project: projectA, task: taskA),
            createTimeEntry(now, workspaceA, "H", duration: 2, project: projectA, task: taskA)
        };

        private static readonly IThreadSafeTimeEntry[] differentTasks =
        {
            createTimeEntry(now, workspaceA, "I", duration: 1, project: projectA, task: taskA),
            createTimeEntry(now, workspaceA, "I", duration: 2, project: projectA, task: taskB)
        };

        public sealed class WhenGroupingIsEnabled
        {
            [Theory]
            [MemberData(nameof(TestData))]
            public void TransformsTimeEntriesIntoACorrectTree(
                IEnumerable<IThreadSafeTimeEntry> timeEntries,
                params CollectionSection<DateTimeOffset, IThreadSafeTimeEntry[]>[] expectedTree)
            {
                var transformedTree = TimeEntriesGrouping.GroupSimilar(timeEntries);

                transformedTree.Should().BeEquivalentTo(expectedTree);
            }

            private static CollectionSection<DateTimeOffset, IThreadSafeTimeEntry[]> dayOf(
                DateTimeOffset date,
                params IThreadSafeTimeEntry[][] groups)
                => new CollectionSection<DateTimeOffset, IThreadSafeTimeEntry[]>(
                    date.LocalDateTime.Date, groups);

            private static IThreadSafeTimeEntry[] groupOf(params IThreadSafeTimeEntry[] timeEntries) => timeEntries;

            public static IEnumerable<object[]> TestData
                => new[]
                {
                    new object[]
                    {
                        singleDay,
                        dayOf(today, groupOf(singleDay))
                    },
                    new object[]
                    {
                        twoWorkspaces,
                        dayOf(today, groupOf(twoWorkspaces[0]), groupOf(twoWorkspaces[1]))
                    },
                    new object[]
                    {
                        differentDescriptions,
                        dayOf(today, groupOf(differentDescriptions[0], differentDescriptions[1]), groupOf(differentDescriptions[2]))
                    },
                    new object[]
                    {
                        multipleDays,
                        dayOf(today, groupOf(multipleDays[0]), groupOf(multipleDays[2])),
                        dayOf(yesterday, groupOf(multipleDays[1]))
                    },
                    new object[]
                    {
                        multipleDaysWithTags,

                        dayOf(today, groupOf(multipleDaysWithTags[0]), groupOf(multipleDaysWithTags[2]), groupOf(multipleDaysWithTags[3], multipleDaysWithTags[4])),
                        dayOf(yesterday, groupOf(multipleDaysWithTags[1]))
                    },
                    new object[]
                    {
                        multipleDaysWithProject,
                        dayOf(today, groupOf(multipleDaysWithProject[0]), groupOf(multipleDaysWithProject[2], multipleDaysWithProject[4]), groupOf(multipleDaysWithProject[3])),
                        dayOf(yesterday, groupOf(multipleDaysWithProject[1]))
                    },
                    new object[]
                    {
                        differentProjects,
                        dayOf(today, groupOf(differentProjects[0]), groupOf(differentProjects[1]))
                    },
                    new object[]
                    {
                        differentTasks,
                        dayOf(today, groupOf(differentTasks[0]), groupOf(differentTasks[1]))
                    },
                    new object[]
                    {
                        sameTasks,
                        dayOf(today, groupOf(sameTasks[0], sameTasks[1]))
                    }
                };
        }

        public sealed class WhenGroupingIsDisabled
        {
            [Theory]
            [MemberData(nameof(TestData))]
            public void TransformsTimeEntriesIntoACorrectTree(
                IEnumerable<IThreadSafeTimeEntry> timeEntries,
                params CollectionSection<DateTimeOffset, IThreadSafeTimeEntry[]>[] expectedTree)
            {
                var transformedTree = TimeEntriesGrouping.WithoutGroupingSimilar(timeEntries);

                transformedTree.Should().BeEquivalentTo(expectedTree);
            }

            private static CollectionSection<DateTimeOffset, IThreadSafeTimeEntry[]> dayOf(
                DateTimeOffset date,
                params IThreadSafeTimeEntry[] timeEntries)
                => new CollectionSection<DateTimeOffset, IThreadSafeTimeEntry[]>(
                    date.LocalDateTime.Date,
                    timeEntries.Select(te => new[] { te }));

            public static IEnumerable<object[]> TestData
                => new[]
                {
                    new object[]
                    {
                        singleDay,
                        dayOf(today, singleDay)
                    },
                    new object[]
                    {
                        twoWorkspaces,
                        dayOf(today, twoWorkspaces)
                    },
                    new object[]
                    {
                        differentDescriptions,
                        dayOf(today, differentDescriptions)
                    },
                    new object[]
                    {
                        multipleDays,
                        dayOf(today, multipleDays[0], multipleDays[2]),
                        dayOf(yesterday, multipleDays[1])
                    },
                    new object[]
                    {
                        multipleDaysWithTags,

                        dayOf(today, multipleDaysWithTags[0], multipleDaysWithTags[2], multipleDaysWithTags[3], multipleDaysWithTags[4]),
                        dayOf(yesterday, multipleDaysWithTags[1])
                    },
                    new object[]
                    {
                        multipleDaysWithProject,
                        dayOf(today, multipleDaysWithProject[0], multipleDaysWithProject[2], multipleDaysWithProject[3], multipleDaysWithProject[4]),
                        dayOf(yesterday, multipleDaysWithProject[1])
                    },
                    new object[]
                    {
                        differentProjects,
                        dayOf(today, differentProjects)
                    },
                    new object[]
                    {
                        differentTasks,
                        dayOf(today, differentTasks)
                    },
                    new object[]
                    {
                        sameTasks,
                        dayOf(today, sameTasks)
                    }
                };
        }

        private static IThreadSafeTimeEntry createTimeEntry(
            DateTimeOffset start,
            IThreadSafeWorkspace workspace,
            string description,
            long duration,
            IThreadSafeProject project = null,
            IThreadSafeTask task = null,
            IThreadSafeTag[] tags = null,
            bool billable = false)
            => new MockTimeEntry
            {
                Start = start,
                Workspace = workspace,
                WorkspaceId = workspace.Id,
                Description = description,
                Duration = duration,
                Project = project,
                ProjectId = project?.Id,
                Task = task,
                TaskId = task?.Id,
                Billable = billable,
                Tags = tags ?? Array.Empty<IThreadSafeTag>(),
                TagIds = tags?.Select(tag => tag.Id) ?? new long[0]
            };
    }
}
