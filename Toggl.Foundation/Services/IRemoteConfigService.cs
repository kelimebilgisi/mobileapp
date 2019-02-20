using System;
using Toggl.Multivac;

namespace Toggl.Foundation.Services
{
    public interface IRemoteConfigService
    {
        IObservable<RatingViewConfiguration> RatingViewConfiguration { get; }
    }
}
