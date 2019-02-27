using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using MvvmCross.Platforms.Android.Presenters.Attributes;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Activities
{
    [MvxActivityPresentation]
    [Activity(Theme = "@style/AppTheme.BlueStatusBar",
        ScreenOrientation = ScreenOrientation.Portrait,
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
    public partial class SelectTagsActivity : ReactiveActivity<SelectTagsViewModel>
    {
        private SelectTagsRecyclerAdapter selectTagsRecyclerAdapter = new SelectTagsRecyclerAdapter();

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.SelectTagsActivity);
            OverridePendingTransition(Resource.Animation.abc_slide_in_bottom, Resource.Animation.abc_fade_out);

            InitializeViews();

            setupLayoutManager(selectTagsRecyclerAdapter);

            ViewModel.Tags
                .Subscribe(replaceTags)
                .DisposedBy(DisposeBag);

            ViewModel.FilterText
                .Select(text => text == string.Empty)
                .DistinctUntilChanged()
                .Subscribe(saveButton.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.FilterText
                .Select(text => text == string.Empty)
                .DistinctUntilChanged()
                .Invert()
                .Subscribe(clearIcon.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            backIcon.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);

            clearIcon.Click += (sender, e) =>
            {
                textField.Text = string.Empty;
            };

            saveButton.Rx()
                .BindAction(ViewModel.Save)
                .DisposedBy(DisposeBag);

            textField.Rx().Text()
                .Subscribe(ViewModel.FilterText)
                .DisposedBy(DisposeBag);

            selectTagsRecyclerAdapter.ItemTapObservable
                .Subscribe(ViewModel.SelectTag.Inputs)
                .DisposedBy(DisposeBag);
        }

        private void setupLayoutManager(SelectTagsRecyclerAdapter adapter)
        {
            var layoutManager = new LinearLayoutManager(this);
            layoutManager.ItemPrefetchEnabled = true;
            layoutManager.InitialPrefetchItemCount = 4;
            selectTagsRecyclerView.SetLayoutManager(layoutManager);
            selectTagsRecyclerView.SetAdapter(adapter);
        }

        public override void Finish()
        {
            base.Finish();
            OverridePendingTransition(Resource.Animation.abc_fade_in, Resource.Animation.abc_slide_out_bottom);
        }

        private void replaceTags(IEnumerable<SelectableTagBaseViewModel> tags)
        {
            selectTagsRecyclerAdapter.Items = tags.ToList();
        }
    }
}
