﻿using System;
using Toggl.Multivac;
using Toggl.Ultrawave.Network;
using Toggl.Ultrawave.Serialization;

namespace Toggl.Ultrawave.Clients
{
    internal sealed class UserClient : BaseClient, IUserClient
    {
        private readonly UserEndpoints endPoints;

        public UserClient(UserEndpoints endPoints, IApiClient apiClient, IJsonSerializer serializer)
            : base(apiClient, serializer)
        {
            this.endPoints = endPoints;
        }

        public IObservable<User> Get(string username, string password)
        {
            var header = GetAuthHeader(username, password);
            var observable = CreateObservable<User>(endPoints.Get, header);
            return observable;
        }
   }
}
