using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Toggl.Foundation.MvvmCross.Interfaces;

namespace Toggl.Giskard.Adapters.DiffingStrategies
{
    public interface IDiffingStrategy<T> where T : IEquatable<T>
    {
        bool AreContentsTheSame(T item, T other);
        bool AreItemsTheSame(T item, T other);
        bool HasStableIds { get; }
        long GetItemId(T item);
    }
}
