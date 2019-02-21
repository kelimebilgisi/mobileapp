using MvvmCross.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Diagnostics;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Extensions.Reactive;
using Toggl.PrimeRadiant.Settings;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    public sealed partial class EditTimeEntryViewModel
    {
        // Constants
        private const int maxTagLength = 30;

        // DI
        private readonly ITimeService timeService;
        private readonly ITogglDataSource dataSource;
        private readonly IDialogService dialogService;
        private readonly IInteractorFactory interactorFactory;
        private readonly IMvxNavigationService navigationService;
        private readonly IAnalyticsService analyticsService;
        private readonly IStopwatchProvider stopwatchProvider;
        private readonly ISchedulerProvider schedulerProvider;
        private readonly IRxActionFactory actionFactory;
        public IOnboardingStorage OnboardingStorage { get; private set; }

        // Performance
        private IStopwatch stopwatchFromCalendar;
        private IStopwatch stopwatchFromMainLog;

        // IDs
        private long? projectId;
        private long? taskId;
        private BehaviorSubject<long> workspaceIdSubject;
        private IThreadSafeTimeEntry originalTimeEntry;

        public long[] TimeEntryIds { get; set; }
        public long TimeEntryId => TimeEntryIds.First();

        // Groups
        public bool IsEditingGroup => TimeEntryIds.Length > 1;
        public int GroupCount => TimeEntryIds.Length;

        // Technical items
        private CompositeDisposable disposeBag;

        // Description
        private BehaviorSubject<bool> isEditingDescriptionSubject;
        public BehaviorRelay<string> Description { get; private set; }

        // Project, task, client
        private BehaviorSubject<ProjectClientTaskInfo> projectClientTaskSubject;
        public IObservable<ProjectClientTaskInfo> ProjectClientTask { get; private set; }


        // Billable
        public IObservable<bool> IsBillableAvailable { get; private set; }

        private BehaviorSubject<bool> isBillableSubject;
        public IObservable<bool> IsBillable { get; private set; }

        // Start TE
        private BehaviorSubject<DateTimeOffset> startTimeSubject;
        public IObservable<DateTimeOffset> StartTime { get; private set; }

        // Duration
        private BehaviorSubject<TimeSpan?> durationSubject;
        public IObservable<TimeSpan> Duration { get; private set; }

        // Stop TE
        public IObservable<DateTimeOffset?> StopTime { get; private set; }

        // Running TE
        public IObservable<bool> IsTimeEntryRunning { get; private set; }

        // GroupDuration
        public TimeSpan GroupDuration { get; private set; }

        // Tags
        private BehaviorSubject<IEnumerable<IThreadSafeTag>> tagsSubject;
        public IObservable<IEnumerable<string>> Tags { get; set; }
        private IEnumerable<long> tagIds
            => tagsSubject.Value.Select(tag => tag.Id);

        // Inaccessibility
        private BehaviorSubject<bool> isInaccessibleSubject;
        public IObservable<bool> IsInaccessible { get; private set; }

        // Sync Error message
        private BehaviorSubject<string> syncErrorMessageSubject;
        public IObservable<string> SyncErrorMessage { get; private set; }
        public IObservable<bool> IsSyncErrorMessageVisible { get; private set; }

        // Preferences
        public IObservable<IThreadSafePreferences> Preferences { get; private set; }

        // Actions
        public UIAction Close { get; private set; }
        public UIAction SelectProject { get; private set; }
        public UIAction SelectTags { get; private set; }
        public UIAction ToggleBillable { get; private set; }
        public InputAction<EditViewTapSource> EditTimes { get; private set; }
        public UIAction StopTimeEntry { get; private set; }
        public UIAction DismissSyncErrorMessage { get; private set; }
        public UIAction Save { get; private set; }
        public UIAction Delete { get; private set; }

        public struct ProjectClientTaskInfo
        {
            public ProjectClientTaskInfo(string project, string projectColor, string client, string task)
            {
                Project = project;
                ProjectColor = projectColor;
                Client = client;
                Task = task;
            }

            public string Project { get; private set; }
            public string ProjectColor { get; private set; }
            public string Client { get; private set; }
            public string Task { get; private set; }

            public bool HasProject => !string.IsNullOrEmpty(Project);

            public static ProjectClientTaskInfo Empty
                => new ProjectClientTaskInfo(null, null, null, null);
        }
    }
}
