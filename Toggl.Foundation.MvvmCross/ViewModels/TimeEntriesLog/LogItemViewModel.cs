using System;
using System.Linq;
using System.Text.RegularExpressions;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels.TimeEntriesLog
{
    public sealed class LogItemViewModel : IDiffable, IEquatable<LogItemViewModel>
    {
        public GroupId GroupId { get; }
        public long[] RepresentedTimeEntriesIds { get; }
        public LogItemVisualizationIntent VisualizationIntent { get; }

        public bool IsBillable { get; }
        public string Description { get; }
        public string ProjectName { get; }
        public string ProjectColor { get; }
        public string ClientName { get; }
        public string TaskName { get; }
        public string Duration { get; }

        public bool HasProject => !string.IsNullOrEmpty(ProjectName);
        public bool HasTags { get; }
        public bool HasDescription => !string.IsNullOrEmpty(Description);

        public bool NeedsSync { get; }
        public bool CanContinue { get; }

        public LogItemViewModel(
            GroupId groupId,
            long[] representedTimeEntriesIds,
            LogItemVisualizationIntent visualizationIntent,
            bool isBillable,
            string description,
            string duration,
            string projectName,
            string projectColor,
            string clientName,
            string taskName,
            bool hasTags,
            bool needsSync,
            bool canSync,
            bool isInaccessible)
        {
            GroupId = groupId;
            RepresentedTimeEntriesIds = representedTimeEntriesIds.OrderBy(id => id).ToArray();
            VisualizationIntent = visualizationIntent;
            IsBillable = isBillable;
            Description = description;
            Duration = duration;
            ProjectName = projectName ?? string.Empty;
            ProjectColor = projectColor ?? string.Empty;
            ClientName = clientName ?? string.Empty;
            TaskName = taskName ?? string.Empty;
            HasTags = hasTags;
            NeedsSync = needsSync;
            CanContinue = canSync && !isInaccessible;

            Identity = isHeader()
                ? $"G_{representedTimeEntriesIds.First()}".GetHashCode()
                : representedTimeEntriesIds.First();
        }

        public bool Equals(LogItemViewModel other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return RepresentedTimeEntriesIds.SequenceEqual(other.RepresentedTimeEntriesIds)
                && IsBillable == other.IsBillable
                && string.Equals(Description, other.Description)
                && string.Equals(ProjectName, other.ProjectName)
                && string.Equals(ProjectColor, other.ProjectColor)
                && string.Equals(ClientName, other.ClientName)
                && string.Equals(TaskName, other.TaskName)
                && Duration.Equals(other.Duration)
                && HasProject == other.HasProject
                && HasTags == other.HasTags
                && NeedsSync == other.NeedsSync
                && CanContinue == other.CanContinue
                && VisualizationIntent == other.VisualizationIntent;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            return obj is LogItemViewModel other && Equals(other);
        }

        public override int GetHashCode()
            => HashCode.From(
                RepresentedTimeEntriesIds.Aggregate(
                    (acc, id) => HashCode.From(acc, id)),
                IsBillable,
                Description,
                ProjectName,
                ProjectColor,
                ClientName,
                TaskName,
                Duration,
                HasProject,
                HasTags,
                NeedsSync,
                CanContinue);

        public static bool operator ==(LogItemViewModel left, LogItemViewModel right) => Equals(left, right);

        public static bool operator !=(LogItemViewModel left, LogItemViewModel right) => !Equals(left, right);
        public long Identity { get; }

        private bool isHeader() => VisualizationIntent == LogItemVisualizationIntent.ExpandedGroupHeader ||
                                   VisualizationIntent == LogItemVisualizationIntent.CollapsedGroupHeader;
    }
}
