using System;
using System.Linq;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.Navigation;
using MvvmCross.ViewModels;
using Toggl.Foundation.Services;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.MvvmCross.ViewModels
{
    [Preserve(AllMembers = true)]
    public class SelectBeginningOfWeekViewModel : MvxViewModel<BeginningOfWeek, BeginningOfWeek>
    {
        private readonly IMvxNavigationService navigationService;

        private BeginningOfWeek defaultResult;

        public SelectableBeginningOfWeekViewModel[] BeginningOfWeekCollection { get; }

        public UIAction Close { get; }
        public InputAction<SelectableBeginningOfWeekViewModel> SelectBeginningOfWeek { get; }

        public SelectBeginningOfWeekViewModel(IMvxNavigationService navigationService, IRxActionFactory rxActionFactory)
        {
            Ensure.Argument.IsNotNull(navigationService, nameof(navigationService));
            Ensure.Argument.IsNotNull(rxActionFactory, nameof(rxActionFactory));

            this.navigationService = navigationService;

            Close = rxActionFactory.FromAsync(close);
            SelectBeginningOfWeek = rxActionFactory.FromAsync<SelectableBeginningOfWeekViewModel>(selectFormat);

            BeginningOfWeekCollection = Enum.GetValues(typeof(BeginningOfWeek))
                            .Cast<BeginningOfWeek>()
                            .Select(beginningOfWeek => new SelectableBeginningOfWeekViewModel(beginningOfWeek, false))
                            .ToArray();
        }

        public override void Prepare(BeginningOfWeek parameter)
        {
            defaultResult = parameter;
            updateSelectedFormat(parameter);
        }

        private Task close() => navigationService.Close(this, defaultResult);

        private Task selectFormat(SelectableBeginningOfWeekViewModel viewModel)
            => navigationService.Close(this, viewModel.BeginningOfWeek);

        private void updateSelectedFormat(BeginningOfWeek selected)
            => BeginningOfWeekCollection.ForEach(viewModel
                => viewModel.Selected = viewModel.BeginningOfWeek == selected);
    }
}
