using System;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Toggl.Foundation.MvvmCross.Parameters;
using Toggl.Foundation.MvvmCross.ViewModels;
using Xunit;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.TestExtensions;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class AboutViewModelTests
    {
        public abstract class AboutViewModelTest : BaseViewModelTests<AboutViewModel>
        {
            protected override AboutViewModel CreateViewModel()
                => new AboutViewModel(NavigationService, RxActionFactory);
        }

        public sealed class TheConstructor : AboutViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useNavigationService,
                bool useRxActionFactory)
            {
                var navigationService = useNavigationService ? NavigationService : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;

                Action tryingToConstructWithEmptyParameters =
                    () => new AboutViewModel(
                        navigationService,
                        rxActionFactory);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheLicensesCommand : AboutViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task NavigatesToTheLicensesViewModel()
            {
                await ViewModel.OpenLicensesView.Execute(TestScheduler);

                await NavigationService.Received().Navigate<LicensesViewModel>();
            }
        }

        public sealed class TheTermsOfServiceCommand : AboutViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task OpensTheBrowserInTheTermsOfServicePage()
            {
                await ViewModel.OpenTermsOfServiceView.Execute(TestScheduler);

                await NavigationService.Received().Navigate<BrowserViewModel, BrowserParameters>(
                    Arg.Is<BrowserParameters>(parameter => parameter.Url == Resources.TermsOfServiceUrl)
                );
            }

            [Fact, LogIfTooSlow]
            public async Task OpensTheBrowserWithTheAppropriateTitle()
            {
                await ViewModel.OpenTermsOfServiceView.Execute(TestScheduler);

                await NavigationService.Received().Navigate<BrowserViewModel, BrowserParameters>(
                    Arg.Is<BrowserParameters>(parameter => parameter.Title == Resources.TermsOfService)
                );
            }
        }

        public sealed class ThePrivacyPolicyCommand : AboutViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task OpensTheBrowserInThePrivacyPolicyPage()
            {
                await ViewModel.OpenPrivacyPolicyView.Execute(TestScheduler);

                await NavigationService.Received().Navigate<BrowserViewModel, BrowserParameters>(
                    Arg.Is<BrowserParameters>(parameter => parameter.Url == Resources.PrivacyPolicyUrl)
                );
            }

            [Fact, LogIfTooSlow]
            public async Task OpensTheBrowserWithTheAppropriateTitle()
            {
                await ViewModel.OpenPrivacyPolicyView.Execute(TestScheduler);

                await NavigationService.Received().Navigate<BrowserViewModel, BrowserParameters>(
                    Arg.Is<BrowserParameters>(parameter => parameter.Title == Resources.PrivacyPolicy)
                );
            }
        }
    }
}
