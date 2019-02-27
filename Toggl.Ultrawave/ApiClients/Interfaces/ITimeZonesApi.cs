using System;
using System.Collections.Generic;

namespace Toggl.Ultrawave.ApiClients
{
    public interface ITimeZonesApi
    {
        IObservable<List<string>> GetAll();
    }
}