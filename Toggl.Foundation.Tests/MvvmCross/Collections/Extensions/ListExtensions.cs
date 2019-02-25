using System;
using System.Collections.Generic;
using System.Linq;
using Toggl.Daneel.ViewSources.Generic;
using Toggl.Foundation.MvvmCross.Collections;

namespace Toggl.Daneel.Tests.Unit.Extensions
{
    public static class ListExtensions
    {
        public static List<TSection> Apply<TSection, THeader, TElement>(this List<TSection> list,
            List<Diffing<TSection, THeader, TElement>.Changeset> changes)
        where TSection : IAnimatableSectionModel<THeader, TElement>, new()
        where THeader : IDiffable
        where TElement : IDiffable, IEquatable<TElement>
        {
            return changes.Aggregate(list, (sections, changeset) =>
            {
                var newSections = changeset.Apply(original: sections);
                return newSections;
            });
        }
    }
}
