using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.Reactive.Testing;
using MvvmCross.ViewModels;
using NSubstitute;
using Toggl.Foundation.Interactors;
using Toggl.Foundation.MvvmCross.Services;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.Tests.Generators;
using Toggl.Foundation.Tests.TestExtensions;
using Toggl.Multivac.Extensions;
using Xunit;

namespace Toggl.Foundation.Tests.MvvmCross.ViewModels
{
    public sealed class SendFeedbackViewModelTests
    {
        public abstract class SendFeedbackViewModelTest : BaseViewModelTests<SendFeedbackViewModel>
        {
            protected override SendFeedbackViewModel CreateViewModel()
                => new SendFeedbackViewModel(NavigationService, InteractorFactory, DialogService, SchedulerProvider, RxActionFactory);
        }

        public sealed class TheConstructor : SendFeedbackViewModelTest
        {
            [Theory, LogIfTooSlow]
            [ConstructorData]
            public void ThrowsIfAnyOfTheArgumentsIsNull(
                bool useNavigationService,
                bool useInteractorFactory,
                bool useDialogService,
                bool useSchedulerProvider,
                bool useRxActionFactory
            )
            {
                var navigationService = useNavigationService ? NavigationService : null;
                var interactorFactory = useInteractorFactory ? InteractorFactory : null;
                var dialogService = useDialogService ? DialogService : null;
                var schedulerProvider = useSchedulerProvider ? SchedulerProvider : null;
                var rxActionFactory = useRxActionFactory ? RxActionFactory : null;

                Action tryingToConstructWithEmptyParameters = ()
                    => new SendFeedbackViewModel(navigationService, interactorFactory, dialogService, schedulerProvider, rxActionFactory);

                tryingToConstructWithEmptyParameters
                    .Should().Throw<ArgumentNullException>();
            }
        }

        public sealed class TheIsFeedbackEmptyObservable : SendFeedbackViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void EmitsValueBasedOnFeedbackText()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.IsFeedbackEmpty.Subscribe(observer);
                viewModel.FeedbackText.OnNext(string.Empty);
                viewModel.FeedbackText.OnNext("some value");
                viewModel.FeedbackText.OnNext(string.Empty);

                TestScheduler.Start();
                observer.Messages.Select(r => r.Value.Value).TakeLast(3).AssertEqual(true, false, true);
            }

            [Fact, LogIfTooSlow]
            public void StartWithTrue()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.IsFeedbackEmpty.Subscribe(observer);

                TestScheduler.Start();
                observer.Messages.First().Value.Value.Should().BeTrue();
            }
        }

        public sealed class TheSendEnabledProperty : SendFeedbackViewModelTest
        {
            [Fact, LogIfTooSlow]
            public void StartsWithFalse()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.SendEnabled.Subscribe(observer);

                TestScheduler.Start();
                observer.SingleEmittedValue().Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void EmitsValueBasedOnFeedbackText()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();
                viewModel.SendEnabled.Subscribe(observer);

                viewModel.FeedbackText.OnNext(string.Empty);
                viewModel.FeedbackText.OnNext("some value");
                viewModel.FeedbackText.OnNext(string.Empty);

                TestScheduler.Start();
                observer.Messages.TakeLast(3).Select(r => r.Value.Value).AssertEqual(false, true, false);
            }

            [Fact, LogIfTooSlow]
            public async Task EmitsFalseWhenFeedbackBeingSent()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();
                viewModel.SendEnabled.Subscribe(observer);

                viewModel.FeedbackText.OnNext("some value");
                await viewModel.Send.Execute(TestScheduler);
                
                observer.LastEmittedValue().Should().BeFalse();
            }
        }

        public sealed class TheIsLoadingProperty : SendFeedbackViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task EmitsTrueWhenFeedbackBeingSent()
            {
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

                var mockedFeedbackInteractor = Substitute.For<IInteractor<IObservable<Unit>>>();
                InteractorFactory.SendFeedback(Arg.Any<string>()).Returns(mockedFeedbackInteractor);
                mockedFeedbackInteractor.Execute().Returns(Observable.Never<Unit>());
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.IsLoading.StartWith(false).Subscribe(observer);
                viewModel.FeedbackText.OnNext("some value");
                await viewModel.Send.Execute(TestScheduler);
                
                observer.LastEmittedValue().Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async Task EmitsFalseAfterNetworkRequestFailed()
            {
                var mockedFeedbackInteractor = Substitute.For<IInteractor<IObservable<Unit>>>();
                InteractorFactory.SendFeedback(Arg.Any<string>()).Returns(mockedFeedbackInteractor);
                mockedFeedbackInteractor.Execute().Returns(Observable.Throw<Unit>(new Exception()));
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.IsLoading.StartWith(true).Subscribe(observer);
                viewModel.FeedbackText.OnNext("some value");
                await viewModel.Send.Execute(TestScheduler);
                
                observer.LastEmittedValue().Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public async Task DoesNotEmitFalseAfterNetworkRequestSucceed()
            {
                var mockedFeedbackInteractor = Substitute.For<IInteractor<IObservable<Unit>>>();
                mockedFeedbackInteractor.Execute().Returns(Observable.Return(Unit.Default));
                InteractorFactory.SendFeedback(Arg.Any<string>()).Returns(mockedFeedbackInteractor);
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.IsLoading.StartWith(true).Subscribe(observer);
                viewModel.FeedbackText.OnNext("some value");
                await viewModel.Send.Execute(TestScheduler);
                
                observer.LastEmittedValue().Should().BeTrue();
            }

            [Fact, LogIfTooSlow]
            public async Task EmitsFalseWhenUserDismissTheView()
            {
                var observer = TestScheduler.CreateObserver<bool>();
                var viewModel = CreateViewModel();

                viewModel.IsLoading.StartWith(true).Subscribe(observer);
                await viewModel.Close.Execute(TestScheduler);
                
                observer.LastEmittedValue().Should().BeFalse();
            }
        }

        public sealed class TheErrorObservable : SendFeedbackViewModelTest
        {
            [Fact, LogIfTooSlow]
            public async Task EmitsNullWhenUserTapOnErrorView()
            {
                var observer = TestScheduler.CreateObserver<Exception>();
                var viewModel = CreateViewModel();

                viewModel.Error.StartWith(new Exception()).Subscribe(observer);
                await viewModel.DismissError.Execute(TestScheduler);

                observer.LastEmittedValue().Should().BeNull();
            }

            [Fact, LogIfTooSlow]
            public async Task PassTheExceptionsForward()
            {
                var mockedFeedbackInteractor = Substitute.For<IInteractor<IObservable<Unit>>>();
                InteractorFactory.SendFeedback(Arg.Any<string>()).Returns(mockedFeedbackInteractor);
                var observer = TestScheduler.CreateObserver<Exception>();
                var viewModel = CreateViewModel();
                var exception = new Exception("nasty exception");

                viewModel.Error.Subscribe(observer);
                mockedFeedbackInteractor.Execute().Returns(Observable.Throw<Unit>(exception));
                viewModel.FeedbackText.OnNext("some value");
                await viewModel.Send.Execute(TestScheduler);
                
                observer.LastEmittedValue().Should().Be(exception);
            }
        }

        public sealed class TheCloseButtonTappedAction : SendFeedbackViewModelTest
        {
            [Fact]
            public async Task SimplyClosesTheViewWhenTextIsEmpty()
            {
                ViewModel.FeedbackText.OnNext(string.Empty);

                await ViewModel.Close.Execute(TestScheduler);

                await NavigationService.Received().Close(ViewModel, false);
            }

            [Property]
            public void ShowsConfirmationDialogWhenFeedbackIsNotEmpty(NonEmptyString feedbackText)
            {
                ViewModel.FeedbackText.OnNext(feedbackText.Get);

                ViewModel.Close.Execute();

                TestScheduler.Start();
                DialogService.Received().ConfirmDestructiveAction(Arg.Any<ActionType>());
            }

            [Property]
            public async Task ClosesTheDialogWithoutSendingFeedbackWhenUserConfirmsDestructiveAction(NonEmptyString feedbackText)
            {
                DialogService.ConfirmDestructiveAction(Arg.Any<ActionType>()).Returns(Observable.Return(true));
                ViewModel.FeedbackText.OnNext(feedbackText.Get);

                await ViewModel.Close.Execute(TestScheduler);

                await NavigationService.Received().Close(ViewModel, false);
            }

            [Property]
            public async Task DoesNotCloseTheDialogWhenUserCancelsDestructiveAction(NonEmptyString feedbackText)
            {
                DialogService.ConfirmDestructiveAction(Arg.Any<ActionType>()).Returns(Observable.Return(false));
                ViewModel.FeedbackText.OnNext(feedbackText.Get);

                await ViewModel.Close.Execute(TestScheduler);

                await NavigationService.DidNotReceive().Close(Arg.Any<IMvxViewModelResult<bool>>(), Arg.Any<bool>());
            }
        }

        public sealed class TheErrorViewTappedAction : SendFeedbackViewModelTest
        {
            [Fact]
            public async Task HidesTheErrorView()
            {
                var observer = TestScheduler.CreateObserver<Exception>();
                InteractorFactory.SendFeedback(Arg.Any<string>())
                    .Execute()
                    .Returns(Observable.Throw<Unit>(new Exception()));
                ViewModel.FeedbackText.OnNext("some feedback");
                ViewModel.Error.Subscribe(observer);

                await ViewModel.Send.Execute(TestScheduler);
                await ViewModel.DismissError.Execute(TestScheduler);
                
                observer.LastEmittedValue().Should().BeNull();
            }
        }

        public sealed class TheSendButtonTappedAction : SendFeedbackViewModelTest
        {
            [Fact]
            public async Task SendsFeedback()
            {
                ViewModel.FeedbackText.OnNext("feedback");
                InteractorFactory.SendFeedback(Arg.Any<string>())
                    .Execute()
                    .Returns(Observable.Return(Unit.Default));

                await ViewModel.Send.Execute(TestScheduler);
                
                await NavigationService.Received().Close(ViewModel, true);
            }

            [Fact]
            public async Task ShowsErrorWhenSendingFeedbackFails()
            {
                var observer = TestScheduler.CreateObserver<Exception>();
                ViewModel.FeedbackText.OnNext("feedback");
                var expectedException = new Exception("damn boom");
                InteractorFactory.SendFeedback(Arg.Any<string>())
                    .Execute()
                    .Returns(Observable.Throw<Unit>(expectedException));
                ViewModel.Error.Subscribe(observer);

                await ViewModel.Send.Execute(TestScheduler);
                
                observer.LastEmittedValue().Should().Be(expectedException);
            }
        }
    }
}
