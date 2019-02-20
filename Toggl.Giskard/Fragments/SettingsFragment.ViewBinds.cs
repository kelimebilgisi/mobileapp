using System;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Giskard.Fragments
{
    public sealed partial class SettingsFragment
    {
        private View helpView;
        private View aboutView;
        private View logoutView;
        private View feedbackView;
        private View manualModeView;
        private View is24hoursModeView;
        private View runningTimerNotificationsView;
        private View stoppedTimerNotificationsView;
        private View avatarContainer;
        private View dateFormatView;
        private View beginningOfWeekView;
        private View durationFormatView;

        private TextView nameTextView;
        private TextView emailTextView;
        private TextView versionTextView;
        private TextView dateFormatTextView;
        private TextView beginningOfWeekTextView;
        private TextView durationFormatTextView;

        private ImageView avatarView;

        private Switch is24hoursModeSwitch;
        private Switch manualModeSwitch;
        private Switch runningTimerNotificationsSwitch;
        private Switch stoppedTimerNotificationsSwitch;

        private RecyclerView workspacesRecyclerView;
        private Toolbar toolbar;

        protected override void InitializeViews(View fragmentView)
        {
            helpView = fragmentView.FindViewById(Resource.Id.SettingsHelpButton);
            aboutView = fragmentView.FindViewById(Resource.Id.SettingsAboutContainer);
            logoutView = fragmentView.FindViewById(Resource.Id.SettingsLogoutButton);
            feedbackView = fragmentView.FindViewById(Resource.Id.SettingsSubmitFeedbackButton);
            manualModeView = fragmentView.FindViewById(Resource.Id.SettingsToggleManualModeView);
            is24hoursModeView = fragmentView.FindViewById(Resource.Id.SettingsIs24HourModeView);
            avatarContainer = fragmentView.FindViewById(Resource.Id.SettingsViewAvatarImageContainer);
            dateFormatView = fragmentView.FindViewById(Resource.Id.SettingsDateFormatView);
            beginningOfWeekView = fragmentView.FindViewById(Resource.Id.SettingsSelectBeginningOfWeekView);
            durationFormatView = fragmentView.FindViewById(Resource.Id.SettingsDurationFormatView);
            runningTimerNotificationsView = fragmentView.FindViewById(Resource.Id.SettingsRunningTimerNotificationsView);
            stoppedTimerNotificationsView = fragmentView.FindViewById(Resource.Id.SettingsStoppedTimerNotificationsView);

            nameTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsNameTextView);
            emailTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsEmailTextView);
            versionTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsAppVersionTextView);
            dateFormatTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsDateFormatTextView);
            beginningOfWeekTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsBeginningOfWeekTextView);
            durationFormatTextView = fragmentView.FindViewById<TextView>(Resource.Id.SettingsDurationFormatTextView);

            avatarView = fragmentView.FindViewById<ImageView>(Resource.Id.SettingsViewAvatarImage);
            manualModeSwitch = fragmentView.FindViewById<Switch>(Resource.Id.SettingsIsManualModeEnabledSwitch);
            is24hoursModeSwitch = fragmentView.FindViewById<Switch>(Resource.Id.SettingsIs24HourModeSwitch);
            runningTimerNotificationsSwitch = fragmentView.FindViewById<Switch>(Resource.Id.SettingsAreRunningTimerNotificationsEnabledSwitch);
            stoppedTimerNotificationsSwitch = fragmentView.FindViewById<Switch>(Resource.Id.SettingsAreStoppedTimerNotificationsEnabledSwitch);

            workspacesRecyclerView = fragmentView.FindViewById<RecyclerView>(Resource.Id.SettingsWorkspacesRecyclerView);
            toolbar = fragmentView.FindViewById<Toolbar>(Resource.Id.Toolbar);
        }
    }
}
