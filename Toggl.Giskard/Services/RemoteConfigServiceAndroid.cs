using System;
using System.Diagnostics;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android.Gms.Tasks;
using Firebase.RemoteConfig;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using GmsTask = Android.Gms.Tasks.Task;

namespace Toggl.Giskard.Services
{
    public class RemoteConfigServiceAndroid : IRemoteConfigService
    {
        private const string delayParameter = "day_count";
        private const string triggerParameter = "criterion";

        [Conditional("DEBUG")]
        private void enableDeveloperModeInDebugModel(FirebaseRemoteConfig remoteConfig)
        {
            var settings = new FirebaseRemoteConfigSettings
                .Builder()
                .SetDeveloperModeEnabled(true)
                .Build();

            remoteConfig.SetConfigSettings(settings);
        }

        public IObservable<RatingViewConfiguration> RatingViewConfiguration
            => Observable.Create<RatingViewConfiguration>(observer =>
            {
                var remoteConfig = FirebaseRemoteConfig.Instance;

                enableDeveloperModeInDebugModel(remoteConfig);

                remoteConfig.SetDefaults(Resource.Xml.RemoteConfigDefaults);

                remoteConfig.Fetch(error =>
                {
                    if (error == null)
                        remoteConfig.ActivateFetched();

                    var configuration = new RatingViewConfiguration(
                        (int)remoteConfig.GetValue(delayParameter).AsLong(),
                        remoteConfig.GetString(triggerParameter).ToRatingViewCriterion()
                    );

                    observer.OnNext(configuration);
                    observer.OnCompleted();
                });

                return Disposable.Empty;
            });
    }

    public class RatingViewCompletionHandler : Java.Lang.Object, IOnCompleteListener
    {
        private FirebaseRemoteConfig remoteConfig;
        private Action<Exception> action;

        public RatingViewCompletionHandler(FirebaseRemoteConfig remoteConfig, Action<Exception> action)
        {
            this.remoteConfig = remoteConfig;
            this.action = action;
        }

        public void OnComplete(GmsTask task)
        {
            action(task.IsSuccessful ? null : task.Exception);
        }
    }

    public static class FirebaseExtensions
    {
        public static void Fetch(this FirebaseRemoteConfig remoteConfig, Action<Exception> action)
        {
            remoteConfig.Fetch().AddOnCompleteListener(new RatingViewCompletionHandler(remoteConfig, action));
        }
    }
}
