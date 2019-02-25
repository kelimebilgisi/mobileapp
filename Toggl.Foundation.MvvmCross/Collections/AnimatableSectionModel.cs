using System;
using System.Collections.Generic;
using System.Linq;

namespace Toggl.Foundation.MvvmCross.Collections
{
    public class AnimatableSectionModel<THeader, TItem> : IAnimatableSectionModel<THeader, TItem>
        where THeader : IDiffable
        where TItem : IDiffable, IEquatable<TItem>
    {
        public THeader Header { get; private set; }
        public List<TItem> Items { get; private set; }

        public long Identity { get; private set; }

        public AnimatableSectionModel()
        {

        }

        public AnimatableSectionModel(THeader header, IEnumerable<TItem> items)
        {
            Header = header;
            Items = items.ToList();
            Identity = Header.Identity;
        }

        public void Initialize(THeader header, IEnumerable<TItem> items)
        {
            Header = header;
            Items = items.ToList();
            Identity = Header.Identity;
        }
    }
}
