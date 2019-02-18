using System;
using System.Linq;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels.TimeEntriesLog
{
    public sealed class GroupId
    {
        private readonly DateTime date;
        private readonly string description;
        private readonly long workspaceId;
        private readonly long? projectId;
        private readonly long? taskId;
        private readonly bool isBillable;
        private readonly long[] tagIds;

        public GroupId(IThreadSafeTimeEntry sample)
        {
            date = sample.Start.LocalDateTime.Date;
            description = sample.Description;
            workspaceId = sample.WorkspaceId;
            projectId = sample.Project?.Id;
            taskId = sample.Task?.Id;
            isBillable = sample.Billable;
            tagIds = sample.TagIds.OrderBy(tag => tag).ToArray();
        }

        public override bool Equals(object obj)
        {
            if (obj is GroupId otherGroup)
            {
                return otherGroup.date == date
                    && otherGroup.description == description
                    && otherGroup.workspaceId == workspaceId
                    && otherGroup.projectId == projectId
                    && otherGroup.taskId == taskId
                    && otherGroup.isBillable == isBillable
                    && otherGroup.tagIds.SequenceEqual(tagIds);
            }

            return false;
        }

        public override int GetHashCode()
        {
            var hashCode = HashCode.From(date, description, workspaceId, projectId, taskId, isBillable);
            return tagIds.Aggregate(hashCode, HashCode.From);
        }
    }
}
