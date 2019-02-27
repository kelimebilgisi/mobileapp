using Android.Support.V7.Widget;
using Android.Widget;

namespace Toggl.Giskard.Activities
{
    public partial class SelectTagsActivity
    {
        private ImageView backIcon;
        private ImageView clearIcon;
        private TextView saveButton;
        private EditText textField;
        private RecyclerView selectTagsRecyclerView;

        protected override void InitializeViews()
        {
            backIcon = FindViewById<ImageView>(Resource.Id.BackIcon);
            clearIcon = FindViewById<ImageView>(Resource.Id.ClearIcon);
            saveButton = FindViewById<TextView>(Resource.Id.SaveButton);
            textField = FindViewById<EditText>(Resource.Id.TextField);
            selectTagsRecyclerView = FindViewById<RecyclerView>(Resource.Id.SelectTagsRecyclerView);
        }
    }
}
