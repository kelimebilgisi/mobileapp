using System;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.Analytics
{
    internal delegate IAnalyticsEvent<string> SyncAnalyticsExtensionsSearchStrategy(Type entityType, IAnalyticsService analyticsService);

    public static class SyncAnalyticsExtensions
    {
        public static IAnalyticsEvent<string> ToSyncErrorAnalyticsEvent(this Type entityType, IAnalyticsService analyticsService)
        {
            var searchStrategy = SearchStrategy ?? DefaultSyncAnalyticsExtensionsSearchStrategy;
            return searchStrategy(entityType, analyticsService);
        }

        internal static SyncAnalyticsExtensionsSearchStrategy SearchStrategy { get; set; }
            = DefaultSyncAnalyticsExtensionsSearchStrategy;

        internal static IAnalyticsEvent<string> DefaultSyncAnalyticsExtensionsSearchStrategy(Type entityType, IAnalyticsService analyticsService)
        {
            if (entityType.ImplementsOrDerivesFrom<IThreadSafeWorkspace>())
                return analyticsService.WorkspaceSyncError;

            if (entityType.ImplementsOrDerivesFrom<IThreadSafeUser>())
                return analyticsService.UserSyncError;

            if (entityType.ImplementsOrDerivesFrom<IThreadSafeWorkspaceFeature>())
                return analyticsService.WorkspaceFeaturesSyncError;

            if (entityType.ImplementsOrDerivesFrom<IThreadSafePreferences>())
                return analyticsService.PreferencesSyncError;

            if (entityType.ImplementsOrDerivesFrom<IThreadSafeTag>())
                return analyticsService.TagsSyncError;

            if (entityType.ImplementsOrDerivesFrom<IThreadSafeClient>())
                return analyticsService.ClientsSyncError;

            if (entityType.ImplementsOrDerivesFrom<IThreadSafeProject>())
                return analyticsService.ProjectsSyncError;

            if (entityType.ImplementsOrDerivesFrom<IThreadSafeTask>())
                return analyticsService.TasksSyncError;

            if (entityType.ImplementsOrDerivesFrom<IThreadSafeTimeEntry>())
                return analyticsService.TimeEntrySyncError;

            throw new ArgumentException($"The entity '{entityType.Name}' has no corresponding analytics events and should not be used.");
        }
    }
}
