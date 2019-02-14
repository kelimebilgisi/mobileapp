using System;
using Android.App;
using Android.Widget;

namespace Toggl.Giskard.ViewHelpers
{
    public sealed class TimePickerListener : Java.Lang.Object, TimePickerDialog.IOnTimeSetListener
    {
        private readonly DateTimeOffset currentTime;
        private readonly Action<DateTimeOffset> onTimePicked;

        public TimePickerListener(DateTimeOffset currentTime, Action<DateTimeOffset> onTimePicked)
        {
            this.currentTime = currentTime;
            this.onTimePicked = onTimePicked;
        }

        public void OnTimeSet(TimePicker view, int hourOfDay, int minute)
        {
            var pickedTime = new DateTimeOffset(currentTime.Year, currentTime.Month, currentTime.Day, hourOfDay, minute, currentTime.Minute, currentTime.Millisecond, currentTime.Offset);
            onTimePicked(pickedTime);
        }
    }
}
