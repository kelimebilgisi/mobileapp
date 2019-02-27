using System;
using System.Windows.Input;
using Android.Runtime;
using Android.Views;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.TemplateSelectors;
using Toggl.Giskard.ViewHolders;

namespace Toggl.Giskard.Adapters
{
    public sealed class SelectTagsRecyclerAdapter : BaseRecyclerAdapter<SelectableTagBaseViewModel>
    {

        private const int selectableTagViewType = 1;
        private const int selectableTagCreationViewType = 2;

        public SelectTagsRecyclerAdapter()
        {
        }

        public SelectTagsRecyclerAdapter(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public override int GetItemViewType(int position)
        {
            var item = GetItem(position);
            switch (item)
            {
                case SelectableTagViewModel _:
                    return selectableTagViewType;
                case SelectableTagCreationViewModel _:
                    return selectableTagCreationViewType;
                default:
                    throw new Exception("Invalid item type");
            }
        }

        protected override BaseRecyclerViewHolder<SelectableTagBaseViewModel> CreateViewHolder(ViewGroup parent, LayoutInflater inflater, int viewType)
        {
            switch (viewType)
            {
                case selectableTagViewType:
                    var inflatedView = inflater.Inflate(Resource.Layout.SelectTagsActivityCell, parent, false);
                    return new TagSelectionViewHolder(inflatedView);
                case selectableTagCreationViewType:
                    var inflatedCreationView = inflater.Inflate(Resource.Layout.EntityCreationActivityCell, parent, false);
                    return new TagCreationSelectionViewHolder(inflatedCreationView);
                default:
                    throw new Exception("Unsupported view type");
            }
        }
    }
}
