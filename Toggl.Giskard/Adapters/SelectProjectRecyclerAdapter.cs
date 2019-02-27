using System;
using System.Collections.Specialized;
using System.Linq;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using MvvmCross.Commands;
using MvvmCross.Platforms.Android.Binding.BindingContext;
using MvvmCross.ViewModels;
using Toggl.Foundation;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Giskard.TemplateSelectors;
using Toggl.Giskard.Views;

namespace Toggl.Giskard.Adapters
{
    public sealed class SelectProjectRecyclerAdapter : BaseRecyclerAdapter<AutocompleteSuggestion>
    {

        private const int projectSuggestionViewType = 1;
        private const int taskSuggestionViewType = 2;
        private const int createEntitySuggestionViewType = 3;

        public SelectProjectRecyclerAdapter()
        {
        }

        public SelectProjectRecyclerAdapter(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        public override int GetItemViewType(int position)
        {
            var item = GetItem(position);
            switch (item)
            {
                case ProjectSuggestion _:
                    return projectSuggestionViewType;
                case TaskSuggestion _:
                    return taskSuggestionViewType;
                case CreateEntitySuggestion _:
                    return createEntitySuggestionViewType;
                default:
                    throw new Exception("Invalid item type");
            }
        }

        protected override BaseRecyclerViewHolder<SelectableTagBaseViewModel> CreateViewHolder(ViewGroup parent, LayoutInflater inflater, int viewType)
        {
            switch (viewType)
            {
                case projectSuggestionViewType:
                    var inflatedView = inflater.Inflate(Resource.Layout.SelectProjectActivityProjectCell, parent, false);
                    return new ProjectSelectionProjectViewHolder(inflatedView);
                case taskSuggestionViewType:
                    var inflatedCreationView = inflater.Inflate(Resource.Layout.SelectProjectActivityTaskCell, parent, false);
                    return new ProjectSelectionTaskViewHolder(inflatedCreationView);
                case createEntitySuggestionViewType:
                    var inflatedCreationView = inflater.Inflate(Resource.Layout.EntityCreationActivityCell, parent, false);
                    return new ProjectCreationSelectionViewHolder(inflatedCreationView);
                default:
                    throw new Exception("Unsupported view type");
            }
        }
    }
}
