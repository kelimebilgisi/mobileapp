
using System;
using System.Linq;
using System.Reactive.Linq;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Giskard.Adapters;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Extensions.Reactive;
using Toggl.Giskard.ViewHolders;
using Toggl.Multivac.Extensions;
using FoundationResources = Toggl.Foundation.Resources;

namespace Toggl.Giskard.Fragments
{
    public sealed partial class SettingsFragment : ReactiveFragment<SettingsViewModel>
    {
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.SettingsFragment, container, false);

            InitializeViews(view);
            setupToolbar();

            var adapter = new SimpleAdapter<SelectableWorkspaceViewModel>(
                Resource.Layout.SettingsFragmentWorkspaceCell,
                WorkspaceSelectionViewHolder.Create
            );
            adapter.ItemTapObservable
                .Subscribe(ViewModel.SelectDefaultWorkspace.Inputs)
                .DisposedBy(DisposeBag);

            workspacesRecyclerView.SetAdapter(adapter);
            workspacesRecyclerView.SetLayoutManager(new LinearLayoutManager(Context));

            versionTextView.Text = ViewModel.Version;

            ViewModel.Name
                .Subscribe(nameTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.Email
                .Subscribe(emailTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.Workspaces
                .Subscribe(adapter.Rx().Items())
                .DisposedBy(DisposeBag);

            ViewModel.IsManualModeEnabled
                .Subscribe(manualModeSwitch.Rx().Checked())
                .DisposedBy(DisposeBag);

            ViewModel.UseTwentyFourHourFormat
                .Subscribe(is24hoursModeSwitch.Rx().Checked())
                .DisposedBy(DisposeBag);

            ViewModel.AreRunningTimerNotificationsEnabled
                .Subscribe(runningTimerNotificationsSwitch.Rx().Checked())
                .DisposedBy(DisposeBag);

            ViewModel.AreStoppedTimerNotificationsEnabled
                .Subscribe(stoppedTimerNotificationsSwitch.Rx().Checked())
                .DisposedBy(DisposeBag);

            ViewModel.DateFormat
                .Subscribe(dateFormatTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.BeginningOfWeek
                .Subscribe(beginningOfWeekTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.DurationFormat
                .Subscribe(durationFormatTextView.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            ViewModel.UserAvatar
                .Select(userImageFromBytes)
                .Subscribe(bitmap =>
                {
                    avatarView.SetImageBitmap(bitmap);
                    avatarContainer.Visibility = ViewStates.Visible;
                })
                .DisposedBy(DisposeBag);

            ViewModel.LoggingOut
                .Subscribe(Context.CancelAllNotifications)
                .DisposedBy(DisposeBag);

            ViewModel.IsFeedbackSuccessViewShowing
                .Subscribe(showFeedbackSuccessToast)
                .DisposedBy(DisposeBag);

            logoutView.Rx()
                .BindAction(ViewModel.TryLogout)
                .DisposedBy(DisposeBag);

            helpView.Rx()
                .BindAction(ViewModel.OpenHelpView)
                .DisposedBy(DisposeBag);

            aboutView.Rx()
                .BindAction(ViewModel.OpenAboutView)
                .DisposedBy(DisposeBag);

            feedbackView.Rx()
                .BindAction(ViewModel.SubmitFeedback)
                .DisposedBy(DisposeBag);

            manualModeView.Rx().Tap()
                .Subscribe(ViewModel.ToggleManualMode)
                .DisposedBy(DisposeBag);

            is24hoursModeView.Rx()
                .BindAction(ViewModel.ToggleTwentyFourHourSettings)
                .DisposedBy(DisposeBag);

            runningTimerNotificationsView.Rx().Tap()
                .Subscribe(ViewModel.ToggleRunningTimerNotifications)
                .DisposedBy(DisposeBag);

            stoppedTimerNotificationsView.Rx().Tap()
                .Subscribe(ViewModel.ToggleStoppedTimerNotifications)
                .DisposedBy(DisposeBag);

            dateFormatView.Rx().Tap()
                .Subscribe(ViewModel.SelectDateFormat.Inputs)
                .DisposedBy(DisposeBag);

            beginningOfWeekView.Rx()
                .BindAction(ViewModel.SelectBeginningOfWeek)
                .DisposedBy(DisposeBag);

            durationFormatView.Rx().Tap()
                .Subscribe(ViewModel.SelectDurationFormat.Inputs)
                .DisposedBy(DisposeBag);

            return view;
        }

        private void showFeedbackSuccessToast(bool succeeeded)
        {
            if (!succeeeded) return;

            var toast = Toast.MakeText(Context, Resource.String.SendFeedbackSuccessMessage, ToastLength.Long);
            toast.SetGravity(GravityFlags.CenterHorizontal | GravityFlags.Bottom, 0, 0);
            toast.Show();
        }

        private Bitmap userImageFromBytes(byte[] imageBytes)
            => BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);

        private void setupToolbar()
        {
            var activity = Activity as AppCompatActivity;
            toolbar.Title = FoundationResources.Settings;
            activity.SetSupportActionBar(toolbar);
        }
    }
}
