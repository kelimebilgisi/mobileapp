using System.Collections.Generic;
using System.Linq;

namespace Toggl.Foundation.MvvmCross.Collections
{
    public class SectionModel<THeader, TItem> : ISectionModel<THeader, TItem>
    {
        public THeader Header { get; private set; }
        public List<TItem> Items { get; private set; }

        public SectionModel()
        {

        }

        public SectionModel(THeader header, IEnumerable<TItem> items)
        {
            Header = header;
            Items = items.ToList();
        }

        public void Initialize(THeader header, IEnumerable<TItem> items)
        {
            Header = header;
            Items = items.ToList();
        }
    }
}
