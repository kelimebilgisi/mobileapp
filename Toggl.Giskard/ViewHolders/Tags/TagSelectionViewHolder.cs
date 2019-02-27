using System;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Toggl.Foundation.MvvmCross.ViewModels;

namespace Toggl.Giskard.ViewHolders
{
    public sealed class TagSelectionViewHolder : BaseRecyclerViewHolder<SelectableTagBaseViewModel>
    {
        private ImageView selectedImageView;
        private TextView nameTextView;

        public TagSelectionViewHolder(View itemView) : base(itemView)
        {
        }

        public TagSelectionViewHolder(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership)
        {
        }

        protected override void InitializeViews()
        {
            selectedImageView = ItemView.FindViewById<ImageView>(Resource.Id.SelectedImageView);
            nameTextView = ItemView.FindViewById<TextView>(Resource.Id.NameTextView);
        }

        protected override void UpdateView()
        {
            nameTextView.Text = Item.Name;
            selectedImageView.Visibility = Item.Selected ? ViewStates.Visible : ViewStates.Gone;
        }
    }
}
