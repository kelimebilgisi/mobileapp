using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels.TimeEntriesLog;
using Toggl.Multivac.Extensions;
using Toggl.PrimeRadiant;
using static Toggl.Foundation.MvvmCross.ViewModels.TimeEntriesLog.LogItemVisualizationIntent;

namespace Toggl.Foundation.MvvmCross.Transformations
{
    internal sealed class TimeEntriesGroupsFlattening
    {
        private readonly ITimeService timeService;
        private readonly HashSet<GroupId> expandedGroups;
        private readonly IDisposable preferencesSubscription;

        private IThreadSafePreferences preferences;

        public TimeEntriesGroupsFlattening(
            ITimeService timeService,
            IObservable<IThreadSafePreferences> preferencesObservable)
        {
            this.timeService = timeService;

            preferencesSubscription =
                preferencesObservable
                    .Subscribe(updatedPreferences =>
                    {
                        preferences = updatedPreferences;
                    });

            expandedGroups = new HashSet<GroupId>();
        }

        public IEnumerable<CollectionSection<DaySummaryViewModel, LogItemViewModel>> Flatten(
            IEnumerable<CollectionSection<DateTimeOffset, IThreadSafeTimeEntry[]>> days)
        {
            return days.Select(flatten);
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

        private CollectionSection<DaySummaryViewModel, LogItemViewModel> flatten(
            CollectionSection<DateTimeOffset, IThreadSafeTimeEntry[]> day)
        {
            var title = DateToTitleString.Convert(day.Header, timeService.CurrentDateTime);
            var duration = totalTrackedTime(day.Items).ToFormattedString(preferences.DurationFormat);
            return new CollectionSection<DaySummaryViewModel, LogItemViewModel>(
                new DaySummaryViewModel(title, duration),
                flattenGroups(day.Items));
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

        private IEnumerable<LogItemViewModel> flattenGroup(
            IThreadSafeTimeEntry[] group)
        {
            var sample = group.First();
            var groupId = new GroupId(sample);

            if (expandedGroups.Contains(groupId))
            {
                if (group.Length > 1)
                {
                    return group
                        .Select(timeEntry => timeEntry.ToViewModel(groupId, GroupItem, preferences.DurationFormat))
                        .Prepend(expandedHeader(groupId, group));
                }

                expandedGroups.Remove(groupId);
            }

            var item = group.Length == 1
                ? sample.ToViewModel(groupId, SingleItem, preferences.DurationFormat)
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
                    preferences.DurationFormat),
                projectName: sample.Project?.Name,
                projectColor: sample.Project?.Color,
                clientName: sample.Project?.Client?.Name,
                taskName: sample.Task?.Name,
                hasTags: sample.Tags.Any(),
                needsSync: group.Any(timeEntry => timeEntry.SyncStatus == SyncStatus.SyncNeeded),
                canSync: group.All(timeEntry => timeEntry.SyncStatus != SyncStatus.SyncFailed),
                isInaccessible: sample.IsInaccessible);
        }
    }
}
