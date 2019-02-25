using System;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Multivac;

namespace Toggl.Foundation.MvvmCross.ViewModels.TimeEntriesLog
{
    public sealed class DaySummaryViewModel : IDiffable
    {
        public string Title { get; }

        public string TotalTrackedTime { get; }

        public DaySummaryViewModel(DateTime day, string title, string totalTrackedTime)
        {
            Title = title;
            TotalTrackedTime = totalTrackedTime;
            Identity = day.ToBinary();
        }

        public long Identity { get; }
    }
}
