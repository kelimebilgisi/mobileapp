using System.Collections.Generic;
using System.Collections.Immutable;

namespace Toggl.Foundation.MvvmCross.Collections.Changes
{
    public struct RemoveMultipleRowsCollectionChange : ICollectionChange
    {
        public IImmutableList<SectionedIndex> RemovedIndexes { get; }

        public RemoveMultipleRowsCollectionChange(IEnumerable<SectionedIndex> removedIndexes)
        {
            RemovedIndexes = removedIndexes.ToImmutableList();
        }
    }
}
