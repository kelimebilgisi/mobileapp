using System;
using System.Reactive.Linq;
using Newtonsoft.Json;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.Ultrawave.Exceptions;
using Toggl.Ultrawave.Helpers;
using Toggl.Ultrawave.Models;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;
using Toggl.Ultrawave.Serialization.Converters;

namespace Toggl.Ultrawave.ApiClients
{
    internal sealed class UserApi : BaseApi, IUserApi
    {
        private const string userAlreadyExistsApiErrorMessage = "user with this email already exists";

        private readonly UserEndpoints endPoints;
        private readonly IJsonSerializer serializer;

        public UserApi(Endpoints endPoints, IApiClient apiClient, IJsonSerializer serializer,
            Credentials credentials)
            : base(apiClient, serializer, credentials, endPoints.LoggedIn)
        {
            this.endPoints = endPoints.User;
            this.serializer = serializer;
        }

        public IObservable<IUser> Get()
            => SendRequest<User>(endPoints.Get, AuthHeader);

        public IObservable<IUser> GetWithGoogle()
            => SendRequest<User>(endPoints.GetWithGoogle, AuthHeader);

        public IObservable<IUser> Update(IUser user)
            => SendRequest(endPoints.Put, AuthHeader, user as User ?? new User(user), SerializationReason.Post);

        public IObservable<string> ResetPassword(Email email)
        {
            var json = $"{{\"email\":\"{email}\"}}";
            return SendRequest(endPoints.ResetPassword, new HttpHeader[0], json)
                .Select(instructions => instructions.Trim('"'));
        }

        public IObservable<IUser> SignUp(
            Email email,
            Password password,
            bool termsAccepted,
            int countryId,
            string timezone
        )
        {
            if (!email.IsValid)
                throw new ArgumentException(nameof(email));

            var dto = new SignUpParameters
            {
                Email = email,
                Password = password,
                Workspace = new WorkspaceParameters
                {
                    InitialPricingPlan = PricingPlans.Free
                },
                TermsAccepted = termsAccepted,
                CountryId = countryId,
                Timezone = timezone
            };
            var json = serializer.Serialize(dto, SerializationReason.Post, null);
            return SendRequest<User>(endPoints.Post, new HttpHeader[0], json)
                .Catch<IUser, BadRequestException>(badRequestException
                    => badRequestException.LocalizedApiErrorMessage == userAlreadyExistsApiErrorMessage
                        ? Observable.Throw<IUser>(new EmailIsAlreadyUsedException(badRequestException))
                        : Observable.Throw<IUser>(badRequestException));
        }

        public IObservable<IUser> SignUpWithGoogle(string googleToken, bool termsAccepted, int countryId, string timezone)
        {
            Ensure.Argument.IsNotNull(googleToken, nameof(googleToken));
            var parameters = new GoogleSignUpParameters
            {
                GoogleAccessToken = googleToken,
                Workspace = new WorkspaceParameters
                {
                    InitialPricingPlan = PricingPlans.Free
                },
                TermsAccepted = termsAccepted,
                CountryId = countryId,
                Timezone = timezone
            };

            var json = serializer.Serialize(parameters, SerializationReason.Post, null);
            return SendRequest<User>(endPoints.PostWithGoogle, new HttpHeader[0], json);
        }

        [Preserve(AllMembers = true)]
        internal class WorkspaceParameters
        {
            public string Name { get; set; } = null;

            public PricingPlans InitialPricingPlan { get; set; }
        }

        [Preserve(AllMembers = true)]
        private class SignUpParameters
        {
            [JsonConverter(typeof(EmailConverter))]
            public Email Email { get; set; }

            [JsonConverter(typeof(PasswordConverter))]
            public Password Password { get; set; }

            public WorkspaceParameters Workspace { get; set; }

            [JsonProperty("tos_accepted")]
            public bool TermsAccepted { get; set; }

            public int CountryId { get; set; }

            public string Timezone { get; set; }
        }

        [Preserve(AllMembers = true)]
        private class GoogleSignUpParameters
        {
            public string GoogleAccessToken { get; set; }

            public WorkspaceParameters Workspace { get; set; }

            [JsonProperty("tos_accepted")]
            public bool TermsAccepted { get; set; }

            public int CountryId { get; set; }

            public string Timezone { get; set; }
        }
    }
}
