using System.Collections.Generic;
using System.Collections.Immutable;

namespace Toggl.Foundation.MvvmCross.Collections.Changes
{
    public struct AddMultipleRowsCollectionChange<T> : ICollectionChange
    {
        public IImmutableList<AddRowCollectionChange<T>> AddedRowChanges { get; }

        public AddMultipleRowsCollectionChange(IEnumerable<AddRowCollectionChange<T>> addedRowChanges)
        {
            AddedRowChanges = addedRowChanges.ToImmutableList();
        }
    }
}
