namespace Toggl.Foundation.MvvmCross.Helper
{
    public enum TemporalInconsistency
    {
        StartTimeAfterCurrentTime,
        StartTimeAfterStopTime,
        StopTimeBeforeStartTime,
        DurationTooLong
    }
}
