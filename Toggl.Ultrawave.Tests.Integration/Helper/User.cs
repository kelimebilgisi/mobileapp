using System;
using System.Net.Http;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Multivac.Extensions;
using Toggl.Ultrawave.Network;
using Toggl.Multivac.Models;
using Toggl.Multivac;
using Toggl.Ultrawave.Tests.Integration.Helper;

namespace Toggl.Ultrawave.Tests.Integration
{
    internal static class User
    {
        public static async Task<IUser> Create()
        {
            var (email, password) = generateEmailPassword();
            return await createUser(email, password);
        }

        public static async Task<(Email email, Password password)> CreateEmailPassword()
        {
            var (email, password) = generateEmailPassword();
            await createUser(email, password);
            return (email, password);
        }

        public static async Task ResetApiToken(IUser user)
        {
            var message = AuthorizedRequestBuilder.CreateRequest(Credentials.WithApiToken(user.ApiToken),
                "https://toggl.space/api/v8/reset_token", HttpMethod.Post);

            using (var client = new HttpClient())
            {
                await client.SendAsync(message);
            }
        }

        private static (Email email, Password password) generateEmailPassword()
            => (RandomEmail.GenerateValid(), "123456".ToPassword());

        private static async Task<IUser> createUser(Email email, Password password)
        {
            var api = Helper.TogglApiFactory.TogglApiWith(Credentials.None);
            var user = await api.User.SignUp(email, password, true, 237, "Europe/Tallinn");

            // This is to make integration tests run slightly slower to prevent SecureChannelFailure
            // errors caused by too many HTTP calls in quick succession in most cases
            // (combined with limiting the XUnit max parallel threads)
            // Empirically, a delay of 0.5s is too short, while 2s sees no further improvements
            //
            // Another issue this is trying to solve is the replication lag between databases.
            // This causes problems when we create a new user and try to make more requests with the credentials
            // of this user which is not known to all the replicas. We then end up with 403 Forbidden
            // if the following request hits a replica which doesn't know the user. There is no perfect
            // solution but we are told that the replication lag should be under one second in most cases.
            await Task.Delay(TimeSpan.FromSeconds(1));

            return user;
        }
    }
}
