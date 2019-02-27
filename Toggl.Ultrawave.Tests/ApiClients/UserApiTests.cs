using System;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.ApiClients;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Models;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;
using Xunit;
using Task = System.Threading.Tasks.Task;

namespace Toggl.Ultrawave.Tests.ApiClients
{
    public class UserApiTests
    {
        public abstract class Base
        {
            private readonly IApiClient apiClient = Substitute.For<IApiClient>();
            private readonly IJsonSerializer jsonSerializer = Substitute.For<IJsonSerializer>();
            private readonly Credentials credentials = Credentials.None;
            private readonly UserApi api;

            public Base()
            {
                api = new UserApi(
                    new Endpoints(ApiEnvironment.Staging),
                    apiClient,
                    jsonSerializer,
                    credentials);
            }

            [Fact, LogIfTooSlow]
            public void SucceedsIfReturnedUserHasApiToken()
            {
                var userWithValidApiToken = userWithApiToken(Guid.NewGuid().ToString());
                setupMocksToReturnUser(userWithValidApiToken);

                var user = CallEndpoint(api).SingleAsync().Wait();

                user.Should().Be(userWithValidApiToken);
            }

            protected abstract IObservable<IUser> CallEndpoint(IUserApi api);

            private Action callingEndpointWithReturnedUser(User user)
            {
                setupMocksToReturnUser(user);

                return () => CallEndpoint(api).Wait();
            }

            private static User userWithApiToken(string apiToken)
                => new User { ApiToken = apiToken };

            private static IResponse successfulResponse()
            {
                var response = Substitute.For<IResponse>();
                response.IsSuccess.Returns(true);
                response.RawData.Returns("some data so it tries to serialize");
                return response;
            }

            private void setupMocksToReturnUser(User user)
            {
                var response = successfulResponse();
                apiClient.Send(Arg.Any<IRequest>()).Returns(taskReturning(response));
                jsonSerializer.Deserialize<User>(Arg.Any<string>()).Returns(user);
            }

            private static Task<T> taskReturning<T>(T value) => Task.Run(() => value);
        }

        public class TheGetMethod : Base
        {
            protected override IObservable<IUser> CallEndpoint(IUserApi api)
                => api.Get();
        }

        public class TheGetWithGoogleMethod : Base
        {
            protected override IObservable<IUser> CallEndpoint(IUserApi api)
                => api.GetWithGoogle();
        }

        public class TheUpdateMethod : Base
        {
            protected override IObservable<IUser> CallEndpoint(IUserApi api)
                => api.Update(new User());
        }

        public class TheSignUpMethod : Base
        {
            protected override IObservable<IUser> CallEndpoint(IUserApi api)
                => api.SignUp(Email.From("a@b.com"), Password.Empty, true, 237, null);
        }

        public class TheSignUpWithGoogleMethod : Base
        {
            protected override IObservable<IUser> CallEndpoint(IUserApi api)
                => api.SignUpWithGoogle("", true, 237, null);
        }
    }
}
