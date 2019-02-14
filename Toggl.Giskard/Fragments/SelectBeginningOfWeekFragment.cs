using System;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using MvvmCross.Droid.Support.V4;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.ViewHolders;
using System.Reactive.Disposables;
using Android.Support.V7.Widget;

namespace Toggl.Giskard.Fragments
{
    [MvxDialogFragmentPresentation(AddToBackStack = true)]
    public sealed partial class SelectBeginningOfWeekFragment : MvxDialogFragment<SelectBeginningOfWeekViewModel>
    {
        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        public SelectBeginningOfWeekFragment() { }

        public SelectBeginningOfWeekFragment(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer) { }

        private SimpleAdapter<SelectableBeginningOfWeekViewModel> adapter;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);
            var view = inflater.Inflate(Resource.Layout.SelectBeginningOfWeekFragment, null);

            initializeViews(view);

            setupRecyclerView();

            adapter.ItemTapObservable
                .Subscribe(ViewModel.SelectBeginningOfWeek.Inputs)
                .DisposedBy(disposeBag);

            return view;
        }

        private void setupRecyclerView()
        {
            recyclerView.SetLayoutManager(new LinearLayoutManager(Context));

            adapter = new SimpleAdapter<SelectableBeginningOfWeekViewModel>(
                Resource.Layout.SelectBeginningOfWeekFragmentCell,
                BeginningOfWeekViewHolder.Create);

            adapter.Items = ViewModel.BeginningOfWeekCollection;

            recyclerView.SetAdapter(adapter);
        }

        public override void OnResume()
        {
            base.OnResume();

            Dialog.Window.SetDefaultDialogLayout(Activity, Context, heightDp: 400);
        }

        public override void OnCancel(IDialogInterface dialog)
        {
            ViewModel.Close.Execute();
        }
    }
}
