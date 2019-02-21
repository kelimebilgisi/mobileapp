using System;
using System.Collections.Generic;
using Toggl.Foundation.MvvmCross.Interfaces;
using Toggl.Foundation.MvvmCross.Reactive;
using Toggl.Giskard.Adapters;

namespace Toggl.Giskard.Extensions.Reactive
{
    public static class RecyclerAdapterExtensions
    {
        public static Action<IList<T>> Items<T>(this IReactive<BaseRecyclerAdapter<T>> reactive) where T : IEquatable<T>
            => collection => reactive.Base.Items = collection;
    }
}
