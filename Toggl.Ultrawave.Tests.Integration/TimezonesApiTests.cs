using System.Reactive.Linq;
using System.Resources;
using System.Threading.Tasks;
using FluentAssertions;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Tests.Integration.BaseTests;
using Xunit;

namespace Toggl.Ultrawave.Tests.Integration
{
    public sealed class TimezonesApiTests
    {
        public sealed class TheGetMethod : EndpointTestBase
        {
            [Fact, LogIfTooSlow]
            public async Task ReturnsNonEmptyCountry()
            {
                var api = TogglApiWith(Credentials.None);

                var timezones = await api.Timezones.GetAll();

                timezones.Should().NotBeNullOrEmpty();
            }

            [Fact, LogIfTooSlow]
            public async Task ShouldMatchOurHardcodedTimezoneJSON()
            {
                var api = TogglApiWith(Credentials.None);

                var timezones = await api.Timezones.GetAll();

                timezones.Should().HaveCount(137, "Consider update the TimezonesJson in Resources.resx");
            }
        }
    }
}
