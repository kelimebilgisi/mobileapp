using System;
using System.Collections.Generic;

namespace Toggl.Foundation.MvvmCross.Collections
{
    public interface IDiffable
    {
        long Identity { get; }
    }

    public interface ISectionModel<THeader, TItem>
    {
        THeader Header { get; }
        List<TItem> Items { get; }
        void Initialize(THeader header, IEnumerable<TItem> items);
    }

    public interface IAnimatableSectionModel<TSection, TItem> : ISectionModel<TSection, TItem>, IDiffable
        where TItem : IDiffable, IEquatable<TItem>
    { }
}
