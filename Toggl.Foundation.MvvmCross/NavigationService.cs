using System;
using System.Threading;
using System.Threading.Tasks;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.MvvmCross.Services;

namespace Toggl.Foundation.MvvmCross
{
    public sealed class NavigationService : MvxNavigationService
    {
        private readonly IAnalyticsService analyticsService;
        private readonly Platform platform;

        public NavigationService(
            IMvxNavigationCache navigationCache,
            IMvxViewModelLoader viewModelLoader,
            IAnalyticsService analyticsService,
            Platform platform) : base(navigationCache, viewModelLoader)
        {
            this.analyticsService = analyticsService;
            this.platform = platform;
        }

        public override Task Navigate(IMvxViewModel viewModel, IMvxBundle presentationBundle = null)
        {
            analyticsService.CurrentPage.Track(viewModel.GetType());
            return base.Navigate(viewModel, presentationBundle);
        }

        public override Task Navigate<TParameter>(IMvxViewModel<TParameter> viewModel, TParameter param, IMvxBundle presentationBundle = null)
        {
            analyticsService.CurrentPage.Track(viewModel.GetType());
            return base.Navigate<TParameter>(viewModel, param, presentationBundle);
        }

        public override Task<TResult> Navigate<TResult>(IMvxViewModelResult<TResult> viewModel, IMvxBundle presentationBundle = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            analyticsService.CurrentPage.Track(viewModel.GetType());
            return base.Navigate<TResult>(viewModel, presentationBundle, cancellationToken);
        }

        public override Task<TResult> Navigate<TParameter, TResult>(IMvxViewModel<TParameter, TResult> viewModel, TParameter param, IMvxBundle presentationBundle = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            analyticsService.CurrentPage.Track(viewModel.GetType());
            return base.Navigate<TParameter, TResult>(viewModel, param, presentationBundle, cancellationToken);
        }

        public override Task Navigate(Type viewModelType, IMvxBundle presentationBundle = null)
        {
            analyticsService.CurrentPage.Track(viewModelType);
            return base.Navigate(viewModelType, presentationBundle);
        }

        public override Task Navigate<TParameter>(Type viewModelType, TParameter param, IMvxBundle presentationBundle = null)
        {
            analyticsService.CurrentPage.Track(viewModelType);
            return base.Navigate<TParameter>(viewModelType, param, presentationBundle);
        }

        public override Task<TResult> Navigate<TResult>(Type viewModelType, IMvxBundle presentationBundle = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            analyticsService.CurrentPage.Track(viewModelType);
            return base.Navigate<TResult>(viewModelType, presentationBundle, cancellationToken);
        }

        public override Task<TResult> Navigate<TParameter, TResult>(Type viewModelType, TParameter param, IMvxBundle presentationBundle = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            analyticsService.CurrentPage.Track(viewModelType);
            return base.Navigate<TParameter, TResult>(viewModelType, param, presentationBundle, cancellationToken);
        }
    }
}
