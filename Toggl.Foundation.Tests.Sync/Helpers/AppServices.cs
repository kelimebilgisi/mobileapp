using System;
using System.Reactive.Concurrency;
using MvvmCross.Navigation;
using NSubstitute;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using IStopwatchProvider = Toggl.Foundation.Diagnostics.IStopwatchProvider;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Foundation.Shortcuts;
using Toggl.Foundation.Sync;
using Toggl.PrimeRadiant;
using Toggl.PrimeRadiant.Settings;
using Toggl.Ultrawave;

namespace Toggl.Foundation.Tests.Sync.Helpers
{
    public sealed class AppServices
    {
        private readonly TimeSpan retryLimit = TimeSpan.FromSeconds(60);
        private readonly TimeSpan minimumTimeInBackgroundForFullSync = TimeSpan.FromMinutes(5);

        private readonly ISyncErrorHandlingService syncErrorHandlingService;

        public IScheduler Scheduler { get; }

        public ITimeService TimeService { get; }

        public IAccessRestrictionStorage AccessRestrictionStorageSubsitute { get; } =
            Substitute.For<IAccessRestrictionStorage>();

        public IMvxNavigationService NavigationServiceSubstitute { get; } =
            Substitute.For<IMvxNavigationService>();

        public IAnalyticsService AnalyticsServiceSubstitute { get; } = Substitute.For<IAnalyticsService>();

        public ILastTimeUsageStorage LastTimeUsageStorageSubstitute { get; } = Substitute.For<ILastTimeUsageStorage>();

        public IStopwatchProvider StopwatchProvider { get; } = Substitute.For<IStopwatchProvider>();

        public ISyncManager SyncManager { get; }

        public AppServices(ITogglApi api, ITogglDatabase database)
        {
            Scheduler = System.Reactive.Concurrency.Scheduler.Default;
            TimeService = new TimeService(Scheduler);

            var errorHandlingService = new ErrorHandlingService(NavigationServiceSubstitute, AccessRestrictionStorageSubsitute);
            syncErrorHandlingService = new SyncErrorHandlingService(errorHandlingService);

            ISyncManager createSyncManager(ITogglDataSource dataSource)
            {
                var syncManager = TogglSyncManager.CreateSyncManager(
                    database,
                    api,
                    dataSource,
                    TimeService,
                    AnalyticsServiceSubstitute,
                    LastTimeUsageStorageSubstitute,
                    Scheduler,
                    StopwatchProvider);

                syncErrorHandlingService.HandleErrorsOf(syncManager);

                return syncManager;
            }

            var togglDataSource = new TogglDataSource(
                api,
                database,
                TimeService,
                createSyncManager,
                AnalyticsServiceSubstitute);

            SyncManager = togglDataSource.SyncManager;
        }
    }
}
