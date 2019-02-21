using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using NSubstitute;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Foundation.MvvmCross.ViewModels.Reports;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.TestExtensions;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models.Reports;
using Toggl.Ultrawave.Models.Reports;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels.Reports
{
    public abstract class ReportsBarChartViewModelTest : BaseViewModelTests<ReportsBarChartViewModel>
    {
        protected ISubject<ITimeEntriesTotals> ReportsSubject { get; } = new Subject<ITimeEntriesTotals>();

        protected ITimeEntriesTotals Report { get; } = Substitute.For<ITimeEntriesTotals>();

        protected ISubject<IThreadSafePreferences> CurrentPreferences { get; } = new Subject<IThreadSafePreferences>();

        protected override ReportsBarChartViewModel CreateViewModel()
        {
            var preferencesDataSource = Substitute.For<ISingletonDataSource<IThreadSafePreferences>>();
            preferencesDataSource.Current.Returns(CurrentPreferences.AsObservable());
            DataSource.Preferences.Returns(preferencesDataSource);

            return new ReportsBarChartViewModel(SchedulerProvider, DataSource.Preferences, ReportsSubject);
        }
    }

    public sealed class TheConstructor
    {
        [Theory]
        [ConstructorData]
        public void ThrowsForNullParameters(
            bool useSchedulerProvider,
            bool usePreferencesDataSource,
            bool useReportsObservable)
        {
            var schedulerProvider = useSchedulerProvider ? Substitute.For<ISchedulerProvider>() : null;
            var preferencesDataSource = usePreferencesDataSource ? Substitute.For<ISingletonDataSource<IThreadSafePreferences>>() : null;
            var reportsObservable = useReportsObservable ? Substitute.For<IObservable<ITimeEntriesTotals>>() : null;

            Action tryingCreateInstance = () =>
                // ReSharper disable once ObjectCreationAsStatement
                new ReportsBarChartViewModel(schedulerProvider, preferencesDataSource, reportsObservable);

            tryingCreateInstance.Should().Throw<ArgumentNullException>();
        }
    }

    public sealed class TheBarsObservable : ReportsBarChartViewModelTest
    {
        private readonly ITimeEntriesTotalsGroup[] groups =
        {
            new TimeEntriesTotalsGroup { Total = TimeSpan.FromHours(13), Billable = TimeSpan.FromHours(3) },
            new TimeEntriesTotalsGroup { Total = TimeSpan.FromHours(11), Billable = TimeSpan.FromHours(4) },
            new TimeEntriesTotalsGroup { Total = TimeSpan.FromHours(10), Billable = TimeSpan.FromHours(5) },
            new TimeEntriesTotalsGroup { Total = TimeSpan.FromHours(9),  Billable = TimeSpan.FromHours(6) },
            new TimeEntriesTotalsGroup { Total = TimeSpan.FromHours(8),  Billable = TimeSpan.FromHours(7) }
        };

        private readonly ITestableObserver<BarViewModel[]> barsObserver;

        public TheBarsObservable()
        {
            barsObserver = TestScheduler.CreateObserver<BarViewModel[]>();
            ViewModel.Bars.Subscribe(barsObserver);
        }

        [Fact]
        public void CalculatesThePercentagesOfDifferentGroups()
        {
            Report.Groups.Returns(groups);

            ReportsSubject.OnNext(Report);

            TestScheduler.Start();
            barsObserver.SingleEmittedValue()
                .Should().BeEquivalentTo(new[]
                {
                    new BarViewModel(3.0 / 14.0, (13.0 - 3.0) / 14.0),
                    new BarViewModel(4.0 / 14.0, (11.0 - 4.0) / 14.0),
                    new BarViewModel(5.0 / 14.0, (10.0 - 5.0) / 14.0),
                    new BarViewModel(6.0 / 14.0, (9.0 - 6.0) / 14.0),
                    new BarViewModel(7.0 / 14.0, (8.0 - 7.0) / 14.0)
                });
        }
    }

    public sealed class TheMaximumHoursPerBarObservable : ReportsBarChartViewModelTest
    {
        private readonly ITimeEntriesTotalsGroup[] groups =
        {
            new TimeEntriesTotalsGroup { Total = TimeSpan.FromHours(13), Billable = TimeSpan.FromHours(3) },
            new TimeEntriesTotalsGroup { Total = TimeSpan.FromHours(11), Billable = TimeSpan.FromHours(4) },
            new TimeEntriesTotalsGroup { Total = TimeSpan.FromHours(10), Billable = TimeSpan.FromHours(5) },
            new TimeEntriesTotalsGroup { Total = TimeSpan.FromHours(9),  Billable = TimeSpan.FromHours(6) },
            new TimeEntriesTotalsGroup { Total = TimeSpan.FromHours(8),  Billable = TimeSpan.FromHours(7) }
        };

        private readonly ITestableObserver<int> maximumHoursPerBarObserver;

        public TheMaximumHoursPerBarObservable()
        {
            maximumHoursPerBarObserver = TestScheduler.CreateObserver<int>();
            ViewModel.MaximumHoursPerBar.Subscribe(maximumHoursPerBarObserver);
        }

        [Fact]
        public void UsesTheMaximumTotalTrackedTimeToCalculateTheMaximumHoursPerBarAsTheNearestLargerEvenNumber()
        {
            Report.Groups.Returns(groups);

            ReportsSubject.OnNext(Report);

            TestScheduler.Start();
            maximumHoursPerBarObserver.LastEmittedValue().Should().Be(14);
        }
    }

    public sealed class TheHorizontalLegendObservable : ReportsBarChartViewModelTest
    {
        [Property]
        public void DoesNotEmitNewValuesForMoreThanSevenDays(PositiveInt days)
        {
            if (days.Get <= 7) return;

            var legendObserver = TestScheduler.CreateObserver<DateTimeOffset[]>();
            var viewModel = CreateViewModel();
            viewModel.HorizontalLegend.Subscribe(legendObserver);
            var groups = Enumerable.Range(0, days.Get)
                .Select(_ => new TimeEntriesTotalsGroup { Billable = TimeSpan.Zero, Total = TimeSpan.Zero })
                .ToArray<ITimeEntriesTotalsGroup>();
            Report.Groups.Returns(groups);

            ReportsSubject.OnNext(Report);

            TestScheduler.Start();
            legendObserver.SingleEmittedValue().Should().BeNull();
        }

        [Theory]
        [InlineData(Resolution.Month)]
        [InlineData(Resolution.Week)]
        public void DoesNotEmitNewValuesForWeeksOrMonthsResolution(Resolution resolution)
        {
            var legendObserver = TestScheduler.CreateObserver<DateTimeOffset[]>();
            ViewModel.HorizontalLegend.Subscribe(legendObserver);
            Report.Groups.Returns(new ITimeEntriesTotalsGroup[0]);
            Report.Resolution.Returns(resolution);

            ReportsSubject.OnNext(Report);

            TestScheduler.Start();
            legendObserver.SingleEmittedValue().Should().BeNull();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        [InlineData(7)]
        public void EmitsASequenceOfDays(int daysCount)
        {
            var start = new DateTimeOffset(2018, 09, 13, 14, 15, 16, TimeSpan.Zero);
            var legendObserver = TestScheduler.CreateObserver<DateTimeOffset[]>();
            ViewModel.HorizontalLegend.Subscribe(legendObserver);
            var groups = Enumerable.Range(0, daysCount)
                .Select(_ => Substitute.For<ITimeEntriesTotalsGroup>())
                .ToArray();
            Report.StartDate.Returns(start);
            Report.Groups.Returns(groups);
            Report.Resolution.Returns(Resolution.Day);

            ReportsSubject.OnNext(Report);

            TestScheduler.Start();
            legendObserver.LastEmittedValue().AssertEqual(
                Enumerable.Range(0, daysCount).Select(n => start.AddDays(n)));
        }
    }

    public sealed class TheDateFormatObservable : ReportsBarChartViewModelTest
    {
        [Fact]
        public void AlwaysUsesCurrentDateFormatFromPreferences()
        {
            var dateFormat = DateFormat.FromLocalizedDateFormat("dd/mm");
            var preferences = Substitute.For<IThreadSafePreferences>();
            preferences.DateFormat.Returns(dateFormat);
            var dateFormatObserver = TestScheduler.CreateObserver<DateFormat>();
            ViewModel.DateFormat.Subscribe(dateFormatObserver);

            CurrentPreferences.OnNext(preferences);

            TestScheduler.Start();
            dateFormatObserver.SingleEmittedValue().Should().Be(dateFormat);
        }
    }
}
