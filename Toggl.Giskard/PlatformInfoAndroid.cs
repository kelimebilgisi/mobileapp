using Android.Icu.Util;
using Toggl.Foundation;

namespace Toggl.Giskard
{
    public sealed class PlatformInfoAndroid : BasePlatformInfo
    {
        public override string TimezoneIdentifier => TimeZone.Default.ID;

        public PlatformInfoAndroid()
            : base("https://support.toggl.com/toggl-timer-for-android/", Platform.Giskard)
        {
        }
    }
}
