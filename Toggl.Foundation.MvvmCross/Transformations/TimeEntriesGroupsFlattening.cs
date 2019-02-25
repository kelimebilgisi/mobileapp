using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels.TimeEntriesLog;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using static Toggl.Foundation.MvvmCross.ViewModels.TimeEntriesLog.LogItemVisualizationIntent;

namespace Toggl.Foundation.MvvmCross.Transformations
{
    using LogGrouping = IGrouping<DateTime, IThreadSafeTimeEntry>;
    using MainLogSection = AnimatableSectionModel<DaySummaryViewModel, LogItemViewModel>;

    internal sealed class TimeEntriesGroupsFlattening
    {
        private readonly ITimeService timeService;
        private readonly HashSet<GroupId> expandedGroups;
        private readonly IObservable<IThreadSafePreferences> preferencesObservable;

        private DurationFormat durationFormat;

        public TimeEntriesGroupsFlattening(
            ITimeService timeService,
            IObservable<IThreadSafePreferences> preferencesObservable)
        {
            this.timeService = timeService;
            this.preferencesObservable = preferencesObservable.ShareReplay();
            expandedGroups = new HashSet<GroupId>();
        }

        public IObservable<IEnumerable<MainLogSection>> Flatten(
            IEnumerable<LogGrouping> days)
        {
            return preferencesObservable
                .Do(preferences => durationFormat = preferences.DurationFormat)
                .Select(preferences =>
                    days.Select(preferences.CollapseTimeEntries
                        ? flatten(bySimilarTimeEntries)
                        : flatten(withJustSingleTimeEntries)
                    )
                );
        }

        public void ToggleGroupExpansion(GroupId groupId)
        {
            if (expandedGroups.Contains(groupId))
            {
                expandedGroups.Remove(groupId);
            }
            else
            {
                expandedGroups.Add(groupId);
            }
        }

        private Func<LogGrouping, MainLogSection> flatten(
            Func<IEnumerable<IThreadSafeTimeEntry>, IEnumerable<IThreadSafeTimeEntry[]>> groupingStrategy)
        {
            return day =>
            {
                var items = groupingStrategy(day);
                var title = DateToTitleString.Convert(day.Key, timeService.CurrentDateTime);
                var duration = totalTrackedTime(items).ToFormattedString(durationFormat);
                return new MainLogSection(
                    new DaySummaryViewModel(day.Key, title, duration),
                    flattenGroups(items)
                );
            };
        }

        private TimeSpan totalTrackedTime(IEnumerable<IThreadSafeTimeEntry[]> groups)
        {
            var trackedSeconds = groups.Sum(group => group.Sum(timeEntry => timeEntry.Duration ?? 0));
            return TimeSpan.FromSeconds(trackedSeconds);
        }

        private IEnumerable<LogItemViewModel> flattenGroups(IEnumerable<IThreadSafeTimeEntry[]> groups)
        {
            return groups.SelectMany(flattenGroup);
        }

        private IEnumerable<LogItemViewModel> flattenGroup(IThreadSafeTimeEntry[] group)
        {
            var sample = group.First();
            var groupId = new GroupId(sample);

            if (expandedGroups.Contains(groupId))
            {
                if (group.Length > 1)
                {
                    return group
                        .Select(timeEntry => timeEntry.ToViewModel(groupId, GroupItem, durationFormat))
                        .Prepend(expandedHeader(groupId, group));
                }

                expandedGroups.Remove(groupId);
            }

            var item = group.Length == 1
                ? sample.ToViewModel(groupId, SingleItem, durationFormat)
                : collapsedHeader(groupId, group);

            return new[] { item };
        }

        private LogItemViewModel collapsedHeader(
            GroupId groupId,
            IThreadSafeTimeEntry[] group)
            => header(groupId, group, CollapsedGroupHeader);

        private LogItemViewModel expandedHeader(
            GroupId groupId,
            IThreadSafeTimeEntry[] group)
            => header(groupId, group, ExpandedGroupHeader);

        private LogItemViewModel header(
            GroupId groupId,
            IThreadSafeTimeEntry[] group,
            LogItemVisualizationIntent visualizationIntent)
        {
            var sample = group.First();
            return new LogItemViewModel(
                groupId: groupId,
                representedTimeEntriesIds: group.Select(timeEntry => timeEntry.Id).ToArray(),
                visualizationIntent: visualizationIntent,
                isBillable: sample.Billable,
                description: sample.Description,
                duration: DurationAndFormatToString.Convert(
                    TimeSpan.FromSeconds(group.Sum(timeEntry => timeEntry.Duration ?? 0)),
                    durationFormat),
                projectName: sample.Project?.Name,
                projectColor: sample.Project?.Color,
                clientName: sample.Project?.Client?.Name,
                taskName: sample.Task?.Name,
                hasTags: sample.Tags.Any(),
                needsSync: group.Any(timeEntry => timeEntry.SyncStatus == SyncStatus.SyncNeeded),
                canSync: group.All(timeEntry => timeEntry.SyncStatus != SyncStatus.SyncFailed),
                isInaccessible: sample.IsInaccessible);
        }

        private static IEnumerable<IThreadSafeTimeEntry[]> bySimilarTimeEntries(
            IEnumerable<IThreadSafeTimeEntry> timeEntries)
        {
            return timeEntries
                .GroupBy(timeEntry => timeEntry, new TimeEntriesComparer())
                .OrderByDescending(group => group.Max(timeEntry => timeEntry.Start))
                .Select(group => group.ToArray());
        }

        private static IEnumerable<IThreadSafeTimeEntry[]> withJustSingleTimeEntries(
            IEnumerable<IThreadSafeTimeEntry> timeEntries)
        {
            return timeEntries.Select(timeEntry => new[] { timeEntry });
        }

        private sealed class TimeEntriesComparer : IEqualityComparer<IThreadSafeTimeEntry>
        {
            public bool Equals(IThreadSafeTimeEntry x, IThreadSafeTimeEntry y)
                => x != null
                   && y != null
                   && x.WorkspaceId == y.WorkspaceId
                   && x.Description == y.Description
                   && x.Project?.Id == y.Project?.Id
                   && x.Task?.Id == y.Task?.Id
                   && x.Billable == y.Billable
                   && haveSameTags(x.TagIds?.ToArray(), y.TagIds?.ToArray());

            public int GetHashCode(IThreadSafeTimeEntry timeEntry)
            {
                var hashCode = HashCode.From(
                    timeEntry.Workspace.Id,
                    timeEntry.Description,
                    timeEntry.Project?.Id,
                    timeEntry.Task?.Id,
                    timeEntry.Billable);

                var tags = timeEntry.TagIds.OrderBy(id => id);
                foreach (var tag in tags)
                {
                    hashCode = HashCode.From(hashCode, tag);
                }

                return hashCode;
            }

            private static bool haveSameTags(long[] a, long[] b)
                => a?.Length == b?.Length && (a?.OrderBy(id => id).SequenceEqual(b.OrderBy(id => id)) ?? true);
        }
    }
}
