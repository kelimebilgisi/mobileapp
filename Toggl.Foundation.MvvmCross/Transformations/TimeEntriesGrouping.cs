using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.Transformations
{
    internal static class TimeEntriesGrouping
    {
        public static IEnumerable<CollectionSection<DateTimeOffset, IThreadSafeTimeEntry[]>> GroupSimilar(
            IEnumerable<IThreadSafeTimeEntry> timeEntries)
        {
            return group(timeEntries, bySimilarTimeEntries);
        }

        public static IEnumerable<CollectionSection<DateTimeOffset, IThreadSafeTimeEntry[]>> WithoutGroupingSimilar(
            IEnumerable<IThreadSafeTimeEntry> timeEntries)
        {
            return group(timeEntries, withJustSingleTimeEntries);
        }

        public static IEnumerable<CollectionSection<DateTimeOffset, IThreadSafeTimeEntry[]>> group(
            IEnumerable<IThreadSafeTimeEntry> timeEntries,
            Func<IEnumerable<IThreadSafeTimeEntry>, IEnumerable<IThreadSafeTimeEntry[]>> groupingStrategy)
        {
            return groupByDate(timeEntries).Select(
                dayGroup => new CollectionSection<DateTimeOffset, IThreadSafeTimeEntry[]>(
                    dayGroup.Key, groupingStrategy(dayGroup)));
        }

        private static IEnumerable<IGrouping<DateTime, IThreadSafeTimeEntry>> groupByDate(
            IEnumerable<IThreadSafeTimeEntry> timeEntries)
        {
            return timeEntries
                .OrderByDescending(te => te.Start)
                .GroupBy(te => te.Start.LocalDateTime.Date);
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
