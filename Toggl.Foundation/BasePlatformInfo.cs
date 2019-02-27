using Xamarin.Essentials;

namespace Toggl.Foundation
{
    public abstract class BasePlatformInfo : IPlatformInfo
    {
        protected BasePlatformInfo(string helpUrl, Platform platform)
        {
            HelpUrl = helpUrl;
            Platform = platform;
        }

        public Platform Platform { get; }

        public string HelpUrl { get; }

        public virtual string TimezoneIdentifier { get; }

        public virtual string Version { get; } = AppInfo.VersionString;

        public virtual string BuildNumber { get; } = AppInfo.BuildString;

        public virtual string PhoneModel { get; } = DeviceInfo.Model;

        public virtual string OperatingSystem { get; } = $"{DeviceInfo.Platform} {DeviceInfo.VersionString}";
    }
}
