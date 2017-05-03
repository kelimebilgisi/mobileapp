﻿﻿using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NSubstitute;
using Toggl.Ultrawave.Helpers;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;
using Xunit;
using static System.Net.HttpStatusCode;

namespace Toggl.Ultrawave.Tests.Clients
{
    public class BaseClientTests
    {
        public class TheGetMethod
        {
            private readonly IApiClient apiClient = Substitute.For<IApiClient>();
            private readonly IJsonSerializer serializer = Substitute.For<IJsonSerializer>();

            [Fact]
            public async Task CreatesRequestWithAppropriateHeaders()
            {
                apiClient.Send(Arg.Any<Request>()).Returns(x => new Response("It lives", true, "text/plain", OK));

                const string username = "susancalvin@psychohistorian.museum";
                const string password = "theirobotmoviesucked123";
                const string expectedHeader = "c3VzYW5jYWx2aW5AcHN5Y2hvaGlzdG9yaWFuLm11c2V1bTp0aGVpcm9ib3Rtb3ZpZXN1Y2tlZDEyMw==";

                var endpoint = Endpoint.Get(ApiUrls.ForEnvironment(ApiEnvironment.Staging), "");
                var client = new TestClient(endpoint, apiClient, serializer);

                client.Get(username, password).Wait();
                await apiClient.Received().Send(Arg.Is<Request>(request => verifyAuthHeader(request, expectedHeader)));
            }

            private static bool verifyAuthHeader(Request request, string expectedHeader)
            {
                var authHeader = request.Headers.ToList().Single();

                if (authHeader.Type != HttpHeader.HeaderType.Auth) return false;
                if (authHeader.Value != expectedHeader) return false;

                return true;
            }
        }
    }
}
