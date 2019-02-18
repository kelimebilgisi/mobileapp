using System;
using Toggl.Foundation.MvvmCross.Interfaces;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    public abstract class SelectableClientBaseViewModel : IDiffableByIdentifier<SelectableClientBaseViewModel>
    {
        public string Name { get; set; }
        public bool Selected { get; set; }

        public SelectableClientBaseViewModel(string name, bool selected)
        {
            Ensure.Argument.IsNotNullOrWhiteSpaceString(name, nameof(name));
            Name = name;
            Selected = selected;
        }

        public override string ToString() => Name;

        public bool Equals(SelectableClientBaseViewModel other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(Name, other.Name) && Selected == other.Selected;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SelectableClientBaseViewModel)obj);
        }

        public override int GetHashCode() => HashCode.From(Name ?? string.Empty, Selected);

        public long Identifier => Name.GetHashCode();
    }

    public sealed class SelectableClientViewModel : SelectableClientBaseViewModel
    {
        public long Id { get; }

        public SelectableClientViewModel(long id, string name, bool selected)
            : base(name, selected)
        {
            Ensure.Argument.IsNotNull(Id, nameof(Id));
            Id = id;
        }
    }

    public sealed class SelectableClientCreationViewModel : SelectableClientBaseViewModel
    {
        public SelectableClientCreationViewModel(string name)
            : base(name, false)
        {
        }
    }
}
