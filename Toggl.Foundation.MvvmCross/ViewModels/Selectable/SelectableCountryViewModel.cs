using System;
using Toggl.Foundation.MvvmCross.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Models;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SelectableCountryViewModel : IDiffableByIdentifier<SelectableCountryViewModel>
    {
        public ICountry Country { get; }

        public bool Selected { get; }

        public SelectableCountryViewModel(ICountry country, bool selected)
        {
            Country = country;
            Selected = selected;
        }

        public override string ToString() => Country.Name;

        public long Identifier => Country.Id;

        public bool Equals(SelectableCountryViewModel other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(Country, other.Country) && Selected == other.Selected;
        }

        public override bool Equals(object obj) => Equals(obj as SelectableCountryViewModel);

        public override int GetHashCode() => HashCode.From(Country, Selected);
    }
}
