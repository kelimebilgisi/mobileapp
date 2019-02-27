using System;

namespace Toggl.Ultrawave.Network
{
    internal struct TimezoneEndpoints
    {
        private readonly Uri baseUrl;

        public TimezoneEndpoints(Uri baseUrl)
        {
            this.baseUrl = baseUrl;
        }

        public Endpoint Get => Endpoint.Get(baseUrl, "timezones");
    }
}