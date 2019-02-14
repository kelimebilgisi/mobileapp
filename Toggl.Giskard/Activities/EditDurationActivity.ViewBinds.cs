using Android.Support.Constraints;
using Android.Views;
using Android.Widget;
using Toggl.Giskard.Views.EditDuration;
using Toolbar = Android.Support.V7.Widget.Toolbar;

namespace Toggl.Giskard.Activities
{
    public partial class EditDurationActivity
    {
        private View startIcon;
        private TextView startLabel;
        private TextView startTimeText;
        private View startDotSeparator;
        private TextView startDateText;
        private View stopIcon;
        private View stopLabel;
        private TextView stopTimeText;
        private TextView stopTimerLabel;
        private View stopDotSeparator;
        private TextView stopDateText;
        private View wheel;
        private WheelBackgroundView wheelBackground;
        private WheelForegroundView wheelForeground;
        private WheelDurationInput wheelNumericInput;
        private Toolbar toolbar;

        protected override void InitializeViews()
        {
            startIcon = FindViewById<View>(Resource.Id.StartIcon);
            startLabel = FindViewById<TextView>(Resource.Id.StartLabel);
            startTimeText = FindViewById<TextView>(Resource.Id.StartTimeText);
            startDotSeparator = FindViewById<View>(Resource.Id.StartDotSeparator);
            startDateText = FindViewById<TextView>(Resource.Id.StartDateText);
            stopIcon = FindViewById<View>(Resource.Id.StopIcon);
            stopLabel = FindViewById<View>(Resource.Id.StopLabel);
            stopTimeText = FindViewById<TextView>(Resource.Id.StopTimeText);
            stopTimerLabel = FindViewById<TextView>(Resource.Id.StopTimerLabel);
            stopDotSeparator = FindViewById<View>(Resource.Id.StopDotSeparator);
            stopDateText = FindViewById<TextView>(Resource.Id.StopDateText);
            wheel = FindViewById<View>(Resource.Id.Wheel);
            wheelBackground = FindViewById<WheelBackgroundView>(Resource.Id.WheelBackground);
            wheelForeground = FindViewById<WheelForegroundView>(Resource.Id.WheelForeground);
            wheelNumericInput = FindViewById<WheelDurationInput>(Resource.Id.WheelDurationInput);
            toolbar = FindViewById<Toolbar>(Resource.Id.Toolbar);
        }
    }
}
