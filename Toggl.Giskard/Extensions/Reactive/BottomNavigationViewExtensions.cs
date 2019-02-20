
using System;
using System.Linq;
using System.Reactive.Linq;
using Android.Support.Design.Widget;
using Android.Views;
using Toggl.Foundation.MvvmCross.Reactive;

namespace Toggl.Giskard.Extensions.Reactive
{
    public static class BottomNavigationViewExtensions
    {
        public static IObservable<IMenuItem> ItemSelected(this IReactive<BottomNavigationView> reactive)
            => Observable
                .FromEventPattern<BottomNavigationView.NavigationItemSelectedEventArgs>(
                    e => reactive.Base.NavigationItemSelected += e,
                    e => reactive.Base.NavigationItemSelected -= e)
                .Select(e => e.EventArgs.Item);
    }
}
