using Toggl.Ultrawave.Network;

namespace Toggl.Foundation
{
    public interface IPlatformInfo
    {
        Platform Platform { get; }

        string HelpUrl { get; }
        string Version { get; }
        string PhoneModel { get; }
        string BuildNumber { get; }
        string OperatingSystem { get; }
        string TimezoneIdentifier { get; }
    }

    public enum Platform
    {
        Daneel,
        Giskard
    }
}
