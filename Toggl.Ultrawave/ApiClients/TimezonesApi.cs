using System;
using System.Collections.Generic;
using Toggl.Multivac;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;


namespace Toggl.Ultrawave.ApiClients
{
    internal sealed class TimezonesApi : BaseApi, ITimeZonesApi
    {
        private readonly TimezoneEndpoints endPoints;

        public TimezonesApi(Endpoints endPoints, IApiClient apiClient, IJsonSerializer serializer, Credentials credentials)
            : base(apiClient, serializer, credentials, endPoints.LoggedIn)
        {
            this.endPoints = endPoints.Timezones;
        }

        public IObservable<List<string>> GetAll()
            => SendRequest<string, string>(endPoints.Get, AuthHeader);
    }
}