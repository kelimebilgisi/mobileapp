using System;
using System.Collections.Generic;
using MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.Interfaces;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SelectableDurationFormatViewModel : IDiffableByIdentifier<SelectableDurationFormatViewModel>
    {
        public long Identifier => DurationFormat.GetHashCode();

        public DurationFormat DurationFormat { get; }

        public bool Selected { get; set; }

        public SelectableDurationFormatViewModel(DurationFormat durationFormat, bool selected)
        {
            DurationFormat = durationFormat;
            Selected = selected;
        }

        public bool Equals(SelectableDurationFormatViewModel other)
            => DurationFormat == other.DurationFormat && Selected == other.Selected;
    }
}
