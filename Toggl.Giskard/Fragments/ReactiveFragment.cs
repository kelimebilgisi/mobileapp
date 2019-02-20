using System.Reactive.Disposables;
using Android.OS;
using Android.Views;
using MvvmCross.Droid.Support.V4;
using MvvmCross.ViewModels;

namespace Toggl.Giskard.Fragments
{
    public abstract class ReactiveFragment<TViewModel> : MvxFragment<TViewModel>
        where TViewModel : class, IMvxViewModel
    {
        protected CompositeDisposable DisposeBag = new CompositeDisposable();

        protected abstract void InitializeViews(View view);

        public override void OnDestroyView()
        {
            base.OnDestroyView();
            DisposeBag.Dispose();
            DisposeBag = new CompositeDisposable();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;
            DisposeBag?.Dispose();
        }
    }
}
