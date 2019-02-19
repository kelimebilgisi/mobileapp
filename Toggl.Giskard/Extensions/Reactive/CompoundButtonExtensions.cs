using System;
using Android.Widget;
using Toggl.Foundation.MvvmCross.Reactive;

namespace Toggl.Giskard.Extensions.Reactive
{
    public static class CompoundButtonExtensions
    {
        public static Action<bool> Checked(this IReactive<CompoundButton> reactive)
            => isChecked => reactive.Base.Checked = isChecked;

        public static Action<bool> CheckedObserver(this IReactive<Switch> reactive, bool ignoreUnchanged = false)
        {
            return isChecked =>
            {
                if (!ignoreUnchanged)
                {
                    reactive.Base.Checked = isChecked;
                    return;
                }

                if (reactive.Base.Checked != isChecked)
                    reactive.Base.Checked = isChecked;
            };
        }
    }
}
