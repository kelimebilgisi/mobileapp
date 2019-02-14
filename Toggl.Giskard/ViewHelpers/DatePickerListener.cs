using System;
using Android.App;
using Android.Widget;

namespace Toggl.Giskard.ViewHelpers
{
    public sealed class DatePickerListener : Java.Lang.Object, DatePickerDialog.IOnDateSetListener
    {
        private readonly DateTimeOffset currentDate;
        private readonly Action<DateTimeOffset> onDatePicked;

        public DatePickerListener(DateTimeOffset currentDate, Action<DateTimeOffset> onDatePicked)
        {
            this.currentDate = currentDate;
            this.onDatePicked = onDatePicked;
        }

        public void OnDateSet(DatePicker view, int year, int month, int dayOfMonth)
        {
            var pickedDate = new DateTimeOffset(year, month + 1, dayOfMonth, currentDate.Hour, currentDate.Minute, currentDate.Minute, currentDate.Millisecond, currentDate.Offset);
            onDatePicked(pickedDate);
        }
    }
}
