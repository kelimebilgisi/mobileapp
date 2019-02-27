using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Toggl.Foundation.Interactors.Timezones;
using Toggl.Foundation.Serialization;
using Xunit;

namespace Toggl.Foundation.Tests.Interactors.Timezones
{
    public sealed class GetSupportedTimezonesInteractorTests
    {
        public sealed class TheExecuteMethod
        {
            [Fact, LogIfTooSlow]
            public async Task ReturnsTheTimezonesApiResponse()
            {
                var interactor = new GetSupportedTimezonesInteractor(new JsonSerializer());

                var returnedTimezones = await interactor.Execute();

                returnedTimezones.Should().NotBeEmpty();
            }
        }
    }
}
