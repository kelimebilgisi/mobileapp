using Android.Views;
using Android.Widget;
using MvvmCross.Droid.Support.V7.AppCompat;
using Toggl.Foundation.MvvmCross.ViewModels;
using Android.Support.Constraints;
using static Toggl.Giskard.Resource.Id;
using Android.Support.V7.Widget;
using Toggl.Giskard.Views;

namespace Toggl.Giskard.Activities
{
    public sealed partial class EditTimeEntryActivity : MvxAppCompatActivity<EditTimeEntryViewModel>
    {
        private View closeButton;
        private View confirmButton;
        private EditText descriptionEditText;

        private Group singleTimeEntryModeViews;
        private Group timeEntriesGroupModeViews;

        private CardView errorContainer;
        private TextView errorText;

        private TextView groupCountTextView;
        private TextView groupDurationTextView;

        private View projectButton;
        private TextView projectPlaceholderLabel;
        private TextView projectTaskClientTextView;

        private View tagsButton;
        private TagsListRecyclerView tagsRecycler;

        private View billableButton;
        private Switch billableSwitch;

        private TextView startTimeTextView;
        private TextView startDateTextView;
        private View changeStartTimeButton;

        private TextView stopTimeTextView;
        private TextView stopDateTextView;
        private View changeStopTimeButton;

        private View stopTimeEntryButton;

        private TextView durationTextView;
        private View changeDurationButton;

        private TextView deleteLabel;
        private View deleteButton;

        private void initializeViews()
        {
            closeButton = FindViewById(CloseButton);
            confirmButton = FindViewById(ConfirmButton);
            descriptionEditText = FindViewById<EditText>(DescriptionEditText);

            singleTimeEntryModeViews = FindViewById<Group>(SingleTimeEntryModeViews);
            timeEntriesGroupModeViews = FindViewById<Group>(TimeEntriesGroupModeViews);

            errorContainer = FindViewById<CardView>(ErrorContainer);
            errorText = FindViewById<TextView>(ErrorText);

            groupCountTextView = FindViewById<TextView>(GroupCount);
            groupDurationTextView = FindViewById<TextView>(GroupDuration);

            projectButton = FindViewById(SelectProjectButton);
            projectPlaceholderLabel = FindViewById<TextView>(ProjectPlaceholderLabel);
            projectTaskClientTextView = FindViewById<TextView>(ProjectTaskClient);

            tagsButton = FindViewById(SelectTagsButton);
            tagsRecycler = FindViewById<TagsListRecyclerView>(TagsRecyclerView);

            billableButton = FindViewById(ToggleBillableButton);
            billableSwitch = FindViewById<Switch>(BillableSwitch);

            startTimeTextView = FindViewById<TextView>(StartTime);
            startDateTextView = FindViewById<TextView>(StartDate);
            changeStartTimeButton = FindViewById(StartTimeButton);

            stopTimeTextView = FindViewById<TextView>(StopTime);
            stopDateTextView = FindViewById<TextView>(StopDate);
            changeStopTimeButton = FindViewById(StopTimeButton);

            stopTimeEntryButton = FindViewById(StopTimeEntryButton);

            durationTextView = FindViewById<TextView>(Duration);
            changeDurationButton = FindViewById(DurationButton);

            deleteLabel = FindViewById<TextView>(DeleteLabel);
            deleteButton = FindViewById(DeleteButton);
        }
    }
}
