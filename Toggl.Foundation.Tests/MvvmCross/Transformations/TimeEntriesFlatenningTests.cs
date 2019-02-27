using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Linq;
using FluentAssertions;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Transformations;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.TimeEntriesLog;
using Toggl.Foundation.Tests.Mocks;
using Toggl.Foundation.Tests.TestExtensions;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.Transformations
{
    public sealed class TimeEntriesFlatenningTests
    {
        private static readonly DateTimeOffset now = new DateTimeOffset(2019, 02, 07, 16, 25, 38, TimeSpan.FromHours(-1));

        private static readonly IThreadSafeWorkspace workspaceA = new MockWorkspace { Id = 1, IsInaccessible = false };
        private static readonly IThreadSafeWorkspace workspaceB = new MockWorkspace { Id = 2, IsInaccessible = false };

        private static readonly IThreadSafeTimeEntry[] singleItemGroup =
            group(createTimeEntry(now, workspaceA, "S", duration: 1));

        private static readonly IThreadSafeTimeEntry[] groupA =
            group(
                createTimeEntry(now, workspaceA, "A", duration: 1),
                createTimeEntry(now, workspaceA, "A", duration: 2),
                createTimeEntry(now, workspaceA, "A", duration: 4));

        private static readonly IThreadSafeTimeEntry[] groupB =
            group(
                createTimeEntry(now, workspaceB, "B", duration: 1),
                createTimeEntry(now, workspaceB, "B", duration: 2),
                createTimeEntry(now, workspaceB, "B", duration: 4));

        private static readonly IThreadSafeTimeEntry[] twoWorkspaces =
            group(
                createTimeEntry(now, workspaceA, "B", duration: 1),
                createTimeEntry(now, workspaceB, "B", duration: 2));

        private static readonly IThreadSafeTimeEntry[] differentDescriptions =
            group(
                createTimeEntry(now, workspaceA, "C1", duration: 1),
                createTimeEntry(now, workspaceA, "C1", duration: 2),
                createTimeEntry(now, workspaceA, "C2", duration: 4)
            );


        private static readonly IThreadSafePreferences preferences = new MockPreferences
        {
            CollapseTimeEntries = true,
            DateFormat = DateFormat.FromLocalizedDateFormat("YYYY-MM-DD")
        };

        private readonly ITimeService timeService;
        private readonly IObservable<IThreadSafePreferences> preferencesObservable;

        public TimeEntriesFlatenningTests()
        {
            timeService = Substitute.For<ITimeService>();
            timeService.CurrentDateTime.Returns(now);

            preferencesObservable = Observable.Return(preferences);
        }

        [Theory]
        [MemberData(nameof(TestData))]
        public void TransformsTimeEntriesIntoACorrectTree(
            IEnumerable<IGrouping<DateTime, IThreadSafeTimeEntry>> log,
            HashSet<GroupId> expandedGroups,
            params ISectionModel<DaySummaryViewModel, LogItemViewModel>[] expectedTree)
        {
            var collapsingStrategy = new TimeEntriesGroupsFlattening(timeService, preferencesObservable);
            expandedGroups.ForEach(collapsingStrategy.ToggleGroupExpansion);

            var scheduler = new TestScheduler();
            var observer = scheduler.CreateObserver<IEnumerable<ISectionModel<DaySummaryViewModel, LogItemViewModel>>>();
            collapsingStrategy.Flatten(log).Subscribe(observer);

            scheduler.Start();

            observer.LastEmittedValue().Should().BeEquivalentTo(expectedTree);
        }

        public static IEnumerable<object[]> TestData
            => new[]
            {
                new object[]
                {
                    new[] { day(now, groupA) },
                    withExpanded(),
                    new[] { logOf(now.DateTime, "Today", "07 sec", collapsed(groupA)) }
                },
                new object[]
                {
                    new[] { day(now, groupA) },
                    withExpanded(groupA),
                    new[] { logOf(now.DateTime, "Today", "07 sec", expanded(groupA)) }
                },
                new object[]
                {
                    new[] { day(now, groupA, groupB) },
                    withExpanded(groupA),
                    new[] { logOf(now.DateTime, "Today", "14 sec", expanded(groupA).Concat(collapsed(groupB))) }
                },
                new object[]
                {
                    new[] { day(now, singleItemGroup) },
                    withExpanded(groupB),
                    new[] { logOf(now.DateTime, "Today", "01 sec", single(singleItemGroup.First())) }
                },
                new object[]
                {
                    new[] { day(now, groupA, singleItemGroup, groupB) },
                    withExpanded(),
                    new[] { logOf(
                        now.DateTime,
                        "Today",
                        "15 sec",
                        collapsed(groupA)
                            .Concat(single(singleItemGroup.First()))
                            .Concat(collapsed(groupB)))
                    }
                },
                new object[]
                {
                    new[] { day(now, groupA, singleItemGroup, groupB) },
                    withExpanded(groupA),
                    new[] { logOf(
                        now.DateTime,
                        "Today",
                        "15 sec",
                        expanded(groupA)
                            .Concat(single(singleItemGroup.First()))
                            .Concat(collapsed(groupB)))
                    }
                },
                new object[]
                {
                    new[] { day(now, groupA, singleItemGroup, groupB) },
                    withExpanded(groupB),
                    new[] { logOf(
                        now.DateTime,
                        "Today",
                        "15 sec",
                        collapsed(groupA)
                            .Concat(single(singleItemGroup.First()))
                            .Concat(expanded(groupB)))
                    }
                },
                new object[]
                {
                    new[] { day(now, groupA, singleItemGroup, groupB) },
                    withExpanded(groupA, groupB),
                    new[] { logOf(
                        now.DateTime,
                        "Today",
                        "15 sec",
                        expanded(groupA)
                            .Concat(single(singleItemGroup.First()))
                            .Concat(expanded(groupB)))
                    }
                },
                new object[]
                {
                    new[] { day(now, twoWorkspaces) },
                    withExpanded(),
                    new[] {
                        logOf(
                            now.DateTime,
                            "Today",
                            "03 sec",
                            single(twoWorkspaces[0])
                                .Concat(single(twoWorkspaces[1]))
                        )
                    }
                },
                new object[]
                {
                    new[] { day(now, differentDescriptions) },
                    withExpanded(),
                    new[]
                    {
                        logOf(
                            now.DateTime,
                            "Today",
                            "07 sec",
                            collapsed(differentDescriptions.Take(2).ToArray())
                                .Concat(single(differentDescriptions[2]))
                            )
                    }
                }
            };

        private static HashSet<GroupId> withExpanded(params IThreadSafeTimeEntry[][] groups)
        {
            var set = new HashSet<GroupId>();
            foreach (var group in groups)
            {
                var groupId = new GroupId(group.First());
                set.Add(groupId);
            }

            return set;
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

        private static IGrouping<DateTime, IThreadSafeTimeEntry> day(
            DateTimeOffset date,
            params IThreadSafeTimeEntry[][] groups)
            => groups
                .SelectMany(group => group)
                .GroupBy(_ => date.DateTime)
                .First();

        private static IThreadSafeTimeEntry[] group(params IThreadSafeTimeEntry[] timeEntries) => timeEntries;

        private static ISectionModel<DaySummaryViewModel, LogItemViewModel> logOf(
            DateTime date,
            string title,
            string trackedTime,
            IEnumerable<LogItemViewModel> items)
            => new SectionModel<DaySummaryViewModel, LogItemViewModel>(
                new DaySummaryViewModel(date, title, trackedTime), items);

        private static IEnumerable<LogItemViewModel> single(IThreadSafeTimeEntry timeEntry)
        {
            yield return timeEntry.ToViewModel(
                new GroupId(timeEntry),
                LogItemVisualizationIntent.SingleItem,
                DurationFormat.Classic);
        }

        private static IEnumerable<LogItemViewModel> collapsed(IThreadSafeTimeEntry[] group)
        {
            yield return header(group, LogItemVisualizationIntent.CollapsedGroupHeader);
        }

        private static IEnumerable<LogItemViewModel> expanded(IThreadSafeTimeEntry[] group)
        {
            yield return header(group, LogItemVisualizationIntent.ExpandedGroupHeader);
            foreach (var timeEntry in group)
            {
                yield return groupItem(timeEntry);
            }
        }

        private static LogItemViewModel header(
            IThreadSafeTimeEntry[] group,
            LogItemVisualizationIntent visualizationIntent)
        {
            var sample = group.First();
            return new LogItemViewModel(
                groupId: new GroupId(sample),
                representedTimeEntriesIds: group.Select(timeEntry => timeEntry.Id).ToArray(),
                visualizationIntent: visualizationIntent,
                isBillable: sample.Billable,
                description: sample.Description,
                duration: DurationAndFormatToString.Convert(
                    TimeSpan.FromSeconds(group.Sum(timeEntry => timeEntry.Duration ?? 0)),
                    DurationFormat.Classic),
                projectName: sample.Project?.Name,
                projectColor: sample.Project?.Color,
                clientName: sample.Project?.Client?.Name,
                taskName: sample.Task?.Name,
                hasTags: sample.Tags.Any(),
                needsSync: group.Any(timeEntry => timeEntry.SyncStatus == SyncStatus.SyncNeeded),
                canSync: group.All(timeEntry => timeEntry.SyncStatus != SyncStatus.SyncFailed),
                isInaccessible: sample.IsInaccessible);
        }

        private static LogItemViewModel groupItem(IThreadSafeTimeEntry timeEntry)
            => timeEntry.ToViewModel(
                new GroupId(timeEntry),
                LogItemVisualizationIntent.GroupItem,
                DurationFormat.Classic);
    }
}
