using System;
using Toggl.PrimeRadiant;

namespace Toggl.Foundation.Models
{
    internal sealed partial class Tag
    {
        internal sealed class Builder
        {
            private const string errorMessage = "You need to set the {0} before building a tag";

            public static Builder Create(long id) => new Builder(id);

            public long Id { get; private set; }

            public long? WorkspaceId { get; private set; }

            public string Name { get; private set; }

            public DateTimeOffset At { get; private set; }

            public DateTimeOffset? ServerDeletedAt { get; private set; }

            public SyncStatus SyncStatus { get; private set; }

            private Builder(long id)
            {
                Id = id;
            }

            public Builder SetWorkspaceId(long worksaceId)
            {
                WorkspaceId = worksaceId;
                return this;
            }

            public Builder SetName(string name)
            {
                Name = name;
                return this;
            }

            public Builder SetAt(DateTimeOffset at)
            {
                At = at;
                return this;
            }

            public Builder SetServerDeletedAt(DateTimeOffset? deletedAt)
            {
                ServerDeletedAt = deletedAt;
                return this;
            }

            public Builder SetSyncStatus(SyncStatus syncStatus)
            {
                SyncStatus = syncStatus;
                return this;
            }

            public Tag Build()
            {
                ensureValidity();
                return new Tag(this);
            }

            private void ensureValidity()
            {
                if (WorkspaceId == null || WorkspaceId == 0)
                    throw new InvalidOperationException(string.Format(errorMessage, nameof(WorkspaceId)));

                if (string.IsNullOrEmpty(Name))
                    throw new InvalidOperationException(string.Format(errorMessage, nameof(Name)));

                if (At == null)
                    throw new InvalidOperationException(string.Format(errorMessage, nameof(At)));
            }
        }

        private Tag(Builder builder)
        {
            Id = builder.Id;
            Name = builder.Name;
            At = builder.At;
            ServerDeletedAt = builder.ServerDeletedAt;
            SyncStatus = builder.SyncStatus;
            WorkspaceId = builder.WorkspaceId.Value;
        }
    }
}
