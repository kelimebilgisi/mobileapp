using System.Collections.Generic;
using System.Collections.Immutable;

namespace Toggl.Foundation.MvvmCross.Collections
{
    public class CollectionSection<THeader, TItem>
    {
        public IImmutableList<TItem> Items { get; set; }
        public THeader Header { get; set; }

        public CollectionSection(THeader header, IEnumerable<TItem> items)
        {
            Header = header;
            Items = items.ToImmutableList();
        }

        public static CollectionSection<THeader, TItem> SingleElement(TItem item, THeader header = default(THeader))
        {
            return new CollectionSection<THeader, TItem>(
                header,
                new[] { item }
            );
        }
    }
}
