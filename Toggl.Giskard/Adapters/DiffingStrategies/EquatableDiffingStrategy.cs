using Android.Support.V7.Widget;
using System;
using System.Linq;

namespace Toggl.Giskard.Adapters.DiffingStrategies
{
    public sealed class EquatableDiffingStrategy<T> : IDiffingStrategy<T>
       where T : IEquatable<T>
    {
        public bool AreContentsTheSame(T item, T other)
        {
            return item.Equals(other);
        }

        public bool AreItemsTheSame(T item, T other)
        {
            return item.Equals(other);
        }

        public long GetItemId(T item) => RecyclerView.NoId;

        public bool HasStableIds => false;
    }
}
