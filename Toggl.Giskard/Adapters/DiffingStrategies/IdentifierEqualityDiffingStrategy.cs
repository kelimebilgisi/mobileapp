using System;
using System.Linq;
using Toggl.Foundation.MvvmCross.Interfaces;
using Toggl.Multivac;

namespace Toggl.Giskard.Adapters.DiffingStrategies
{
    public sealed class IdentifierEqualityDiffingStrategy<T> : IDiffingStrategy<T>
        where T : IEquatable<T>
    {
        public IdentifierEqualityDiffingStrategy()
        {
            Ensure.Argument.TypeImplementsOrInheritsFromType(
                derivedType: typeof(T),
                baseType: typeof(IDiffableByIdentifier<T>));
        }

        public bool AreContentsTheSame(T item, T other)
        {
            return item.Equals(other);
        }

        public bool AreItemsTheSame(T item, T other)
        {
            var itemDiffable = (IDiffableByIdentifier<T>)item;
            var otherDiffable = (IDiffableByIdentifier<T>)item;

            return itemDiffable.Identifier == otherDiffable.Identifier;
        }

        public long GetItemId(T item) => ((IDiffableByIdentifier<T>)item).Identifier;

        public bool HasStableIds => true;
    }
}
