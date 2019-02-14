using System;
using System.Globalization;
using System.Reactive.Linq;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using PropertyChanged;
using Toggl.Foundation.Helper;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Multivac;
using System.Reactive.Subjects;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.MvvmCross.Helper;
using static Toggl.Foundation.MvvmCross.Helper.TemporalInconsistency;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public sealed class SelectTimeViewModel
        : MvxViewModel<SelectTimeParameters, SelectTimeResultsParameters>
    {
        public const int StartTimeTab = 0;
        public const int StopTimeTab = 1;
        public const int DurationTab = 2;

        private readonly ISubject<TemporalInconsistency> temporalInconsistencySubject = new Subject<TemporalInconsistency>();
        private readonly ISubject<bool> calendarModeSubject = new BehaviorSubject<bool>(false);

        private readonly ITogglDataSource dataSource;
        private readonly IMvxNavigationService navigationService;
        private readonly IInteractorFactory interactorFactory;
        private readonly ITimeService timeService;
        private IDisposable timeServiceDisposable;
        private bool isViewModelPrepared;
        private TimeSpan? editingDuration;

        public DateTimeOffsetRange StartTimeBoundaries { get; set; }

        public DateTimeOffsetRange StopTimeBoundaries { get; set; }

        public DateTimeOffset CurrentDateTime { get; set; }

        public DateTimeOffset StartTime { get; set; }

        [DependsOn(nameof(CurrentDateTime))]
        public DateTimeOffset? StopTime { get; set; }

        [DependsOn(nameof(StartTime), nameof(StopTime))]
        public TimeSpan Duration
            => (StopTime ?? CurrentDateTime) - StartTime;

        public DateFormat DateFormat { get; set; }

        public TimeFormat TimeFormat { get; set; }

        public int StartingTabIndex { get; private set; }

        public DateTimeOffset StopTimeOrCurrent => StopTime ?? CurrentDateTime;

        public string StartTimeAmPmLabel =>
            TimeFormat.IsTwentyFourHoursFormat ? "" : StartTime.ToString("tt", CultureInfo.InvariantCulture);

        public string StopTimeAmPmLabel =>
            TimeFormat.IsTwentyFourHoursFormat ? "" : StopTimeOrCurrent.ToString("tt", CultureInfo.InvariantCulture);

        public IObservable<TemporalInconsistency> TemporalInconsistencyDetected { get; }
        public IObservable<bool> IsCalendarViewObservable { get; }

        public IObservable<bool> Is24HoursModeObservable { get; private set; }

        public BeginningOfWeek BeginningOfWeek { get; private set; }

        public bool IsEditingDuration { get; set; }

        [DependsOn(nameof(StopTime))]
        public bool IsTimeEntryStopped => StopTime.HasValue;

        public IMvxCommand StopTimeEntryCommand { get; }

        public IMvxAsyncCommand CancelCommand { get; }

        public IMvxAsyncCommand SaveCommand { get; }

        public IMvxCommand ToggleClockCalendarModeCommand { get; }

        public IMvxCommand<int> IncreaseDurationCommand { get; }

        public IMvxCommand FocusDurationCommand { get; }

        public IMvxCommand UnfocusDurationCommand { get; }

        public bool IsCalendarView { get; set; }

        public int CurrentTab { get; set; }

        public bool IsOnStartTimeTab => CurrentTab == StartTimeTab;

        public bool IsOnStopTimeTab => CurrentTab == StopTimeTab;

        public bool IsOnDurationTab => CurrentTab == DurationTab;

        public bool IsDurationVisible { get; set; }

        public TimeSpan EditingDuration
        {
            get => editingDuration ?? Duration;
            set
            {
                if (StopTime.HasValue)
                {
                    StopTime = StartTime + value;
                }
                else
                {
                    StartTime = CurrentDateTime - value;
                }

                editingDuration = (StopTime ?? CurrentDateTime) - StartTime;
            }
        }

        [DependsOn(nameof(CurrentDateTime))]
        public bool IsRunningTimeEntryAM => CurrentDateTime.Hour < 12;

        public SelectTimeViewModel(
            ITogglDataSource dataSource,
            IMvxNavigationService navigationService,
            IInteractorFactory interactorFactory,
            ITimeService timeService)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(interactorFactory, nameof(interactorFactory));
            Ensure.Argument.IsNotNull(timeService, nameof(timeService));

            this.navigationService = navigationService;
            this.timeService = timeService;
            this.interactorFactory = interactorFactory;
            this.dataSource = dataSource;

            CancelCommand = new MvxAsyncCommand(cancel);
            SaveCommand = new MvxAsyncCommand(save);

            IncreaseDurationCommand = new MvxCommand<int>(increaseDuration);

            FocusDurationCommand = new MvxCommand(focusDurationCommand);
            UnfocusDurationCommand = new MvxCommand(unfocusDurationCommand);

            StopTimeEntryCommand = new MvxCommand(stopTimeEntry);

            ToggleClockCalendarModeCommand = new MvxCommand(togglClockCalendarMode);

            TemporalInconsistencyDetected = temporalInconsistencySubject.AsObservable();
            IsCalendarViewObservable = calendarModeSubject.AsObservable();
        }

        public override void Prepare(SelectTimeParameters parameter)
        {
            StartTime = parameter.Start.ToLocalTime();
            StopTime = parameter.Stop?.ToLocalTime();

            DateFormat = parameter.DateFormat;
            TimeFormat = parameter.TimeFormat;

            StartingTabIndex = parameter.StartingTabIndex;
            IsCalendarView = parameter.ShouldStartOnCalendar;
            calendarModeSubject.OnNext(IsCalendarView);

            BeginningOfWeek = parameter.BeginningOfWeek;

            initializeTimeConstraints();
            isViewModelPrepared = true;
        }

        public override async Task Initialize()
        {
            await base.Initialize();

            if (!StopTime.HasValue)
            {
                timeServiceDisposable =
                    timeService.CurrentDateTimeObservable
                               .StartWith(timeService.CurrentDateTime)
                               .Subscribe(currentDateTime => CurrentDateTime = currentDateTime);
            }

            Is24HoursModeObservable = interactorFactory
                .GetPreferences()
                .Execute()
                .Select(pref => pref.TimeOfDayFormat.IsTwentyFourHoursFormat);
        }

        private void increaseDuration(int minutes)
        {
            editingDuration = Duration;
            EditingDuration += TimeSpan.FromMinutes(minutes);
        }

        private void focusDurationCommand()
        {
            IsEditingDuration = true;
        }

        private void unfocusDurationCommand()
        {
            IsEditingDuration = false;
        }

        private void togglClockCalendarMode()
        {
            IsCalendarView = !IsCalendarView;
            calendarModeSubject.OnNext(IsCalendarView);
        }

        private void stopTimeEntry()
        {
            StopTime = CurrentDateTime;
            IsCalendarView = false;
            calendarModeSubject.OnNext(IsCalendarView);
        }

        private void initializeTimeConstraints()
        {
            StopTimeBoundaries = new DateTimeOffsetRange(
                StartTime,
                StartTime + Constants.MaxTimeEntryDuration);

            if (StopTime.HasValue)
            {
                StartTimeBoundaries = new DateTimeOffsetRange(
                    StopTime.Value - Constants.MaxTimeEntryDuration,
                    StopTime.Value);
            }
            else
            {
                StartTimeBoundaries = new DateTimeOffsetRange(
                    Constants.EarliestAllowedStartTime,
                    Constants.LatestAllowedStartTime);
            }
        }

        private void OnStartTimeChanged()
        {
            if (!isViewModelPrepared)
                return;

            if (StopTime.HasValue)
            {
                if (StopTime < StartTime)
                {
                    StartTime = StopTime.Value;
                    temporalInconsistencySubject.OnNext(StartTimeAfterStopTime);
                }

                if (StopTime.Value - StartTime > Constants.MaxTimeEntryDuration)
                {
                    StartTime = StopTime.Value - Constants.MaxTimeEntryDuration;
                    temporalInconsistencySubject.OnNext(DurationTooLong);
                }
            }
            else
            {
                if (StartTime > CurrentDateTime)
                {
                    StartTime = CurrentDateTime;
                    temporalInconsistencySubject.OnNext(StartTimeAfterCurrentTime);
                }
            }

            StopTimeBoundaries = new DateTimeOffsetRange(
                StartTime,
                StartTime + Constants.MaxTimeEntryDuration);
        }

        private void OnStopTimeChanged()
        {
            if (!isViewModelPrepared)
                return;

            if (StopTime.HasValue)
            {
                if (StopTime < StartTime)
                {
                    StopTime = StartTime;
                    temporalInconsistencySubject.OnNext(StopTimeBeforeStartTime);
                }

                if (StopTime.Value - StartTime > Constants.MaxTimeEntryDuration)
                {
                    StopTime = StartTime + Constants.MaxTimeEntryDuration;
                    temporalInconsistencySubject.OnNext(DurationTooLong);
                }

                StartTimeBoundaries = new DateTimeOffsetRange(
                    StopTime.Value - Constants.MaxTimeEntryDuration,
                    StopTime.Value);
            }
            else
            {
                StartTimeBoundaries = new DateTimeOffsetRange(
                    Constants.EarliestAllowedStartTime,
                    Constants.LatestAllowedStartTime);
            }
        }

        public override void ViewDestroy(bool viewFinishing)
        {
            base.ViewDestroy(viewFinishing);
            timeServiceDisposable?.Dispose();
        }

        private async Task cancel()
            => await navigationService.Close(this, null);

        private async Task save()
        {
            var results = new SelectTimeResultsParameters(StartTime.ToUniversalTime(), StopTime?.ToUniversalTime());
            await navigationService.Close(this, results);
        }
    }
}
