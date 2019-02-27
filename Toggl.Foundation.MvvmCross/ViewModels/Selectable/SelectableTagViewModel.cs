using Toggl.Foundation.MvvmCross.Interfaces;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public abstract class SelectableTagBaseViewModel : IDiffableByIdentifier<SelectableTagBaseViewModel>
    {
        public string Name { get; }
        public bool Selected { get; }

        public long WorkspaceId { get; }

        public SelectableTagBaseViewModel(string name, bool selected, long workspaceId)
        {
            Ensure.Argument.IsNotNullOrWhiteSpaceString(name, nameof(name));
            Name = name;
            Selected = selected;
            WorkspaceId = workspaceId;
        }

        public override string ToString() => Name;

        public bool Equals(SelectableTagBaseViewModel other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name)
                && WorkspaceId == other.WorkspaceId
                && Selected == other.Selected;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SelectableTagBaseViewModel)obj);
        }

        public override int GetHashCode() => HashCode.From(Name ?? string.Empty, WorkspaceId);

        public long Identifier => GetHashCode();
    }

    public sealed class SelectableTagViewModel : SelectableTagBaseViewModel
    {
        public long Id { get; }

        public SelectableTagViewModel(
            long id,
            string name,
            bool selected,
            long workspaceId
        )
            : base(name, selected, workspaceId)
        {
            Ensure.Argument.IsNotNull(id, nameof(id));
            Id = id;
        }
    }

    public sealed class SelectableTagCreationViewModel : SelectableTagBaseViewModel
    {
        public SelectableTagCreationViewModel(string name, long workspaceId)
            : base(name, false, workspaceId)
        {
        }
    }
}
