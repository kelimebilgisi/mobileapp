using System;
using System.Reactive.Linq;
using Android.Widget;
using Toggl.Foundation.MvvmCross.Reactive;
using static Android.Widget.CompoundButton;

namespace Toggl.Giskard.Extensions.Reactive
{
    public static class CompoundButtonExtensions
    {
        public static IObservable<bool> Checked(this IReactive<Switch> reactive)
          => Observable
              .FromEventPattern<CheckedChangeEventArgs>(e => reactive.Base.CheckedChange += e, e => reactive.Base.CheckedChange -= e)
              .Select(args => ((Switch)args.Sender).Checked);

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
