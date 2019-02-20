using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Firebase.RemoteConfig;
using Toggl.Foundation.Exceptions;
using Toggl.Foundation.Services;
using Toggl.Multivac;

namespace Toggl.Daneel.Services
{
    public sealed class RemoteConfigServiceIos : IRemoteConfigService
    {
        public void SetupDefaults(string plistName)
        {
            var remoteConfig = RemoteConfig.SharedInstance;
            remoteConfig.SetDefaults(plistFileName: plistName);
        }

        public IObservable<RatingViewConfiguration> RatingViewConfiguration
            => Observable.Create<RatingViewConfiguration>(observer =>
            {
                var remoteConfig = RemoteConfig.SharedInstance;
                remoteConfig.Fetch((status, error) =>
                {
                    if (error == null)
                        remoteConfig.ActivateFetched();

                    var configuration = new RatingViewConfiguration(
                        remoteConfig["day_count"].NumberValue.Int32Value,
                        remoteConfig["criterion"].StringValue.ToRatingViewCriterion()
                    );
                    observer.OnNext(configuration);
                    observer.OnCompleted();
                });
                return Disposable.Empty;
            });
    }
}
