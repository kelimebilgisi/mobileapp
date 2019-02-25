using MvvmCross;
using MvvmCross.Platforms.Ios.Presenters.Attributes;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation.MvvmCross.ViewModels.Settings;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Color = Toggl.Foundation.MvvmCross.Helper.Color;
using FoundationResources = Toggl.Foundation.Resources;

namespace Toggl.Daneel.ViewControllers.Settings
{
    [MvxChildPresentation]
    public sealed partial class CalendarSettingsViewController : ReactiveViewController<CalendarSettingsViewModel>
    {
        private const int tableViewHeaderHeight = 106;

        private readonly ISchedulerProvider schedulerProvider;

        public CalendarSettingsViewController() : base(nameof(CalendarSettingsViewController))
        {
            schedulerProvider = Mvx.Resolve<ISchedulerProvider>();
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            NavigationItem.Title = FoundationResources.CalendarSettingsTitle;

            var header = CalendarSettingsTableViewHeader.Create();
            UserCalendarsTableView.TableHeaderView = header;
            header.TranslatesAutoresizingMaskIntoConstraints = false;
            header.HeightAnchor.ConstraintEqualTo(tableViewHeaderHeight).Active = true;
            header.WidthAnchor.ConstraintEqualTo(UserCalendarsTableView.WidthAnchor).Active = true;
            header.SetCalendarPermissionStatus(ViewModel.PermissionGranted);

            var source = new SelectUserCalendarsTableViewSource(UserCalendarsTableView);
            source.SectionHeaderBackgroundColor = Color.Settings.Background.ToNativeColor();
            UserCalendarsTableView.Source = source;

            ViewModel.Calendars
                .Subscribe(UserCalendarsTableView.Rx().ReloadSections(source))
                .DisposedBy(DisposeBag);

            header.EnableCalendarAccessTapped
                .Subscribe(ViewModel.RequestAccess.Inputs)
                .DisposedBy(DisposeBag);

            source.Rx().ModelSelected()
                .Subscribe(ViewModel.SelectCalendar.Inputs)
                .DisposedBy(DisposeBag);
        }
    }
}

