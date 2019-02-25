using System.Reactive;
using System.Threading.Tasks;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.Views.Settings;
using Toggl.Daneel.ViewSources.Generic.TableView;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public partial class SelectBeginningOfWeekViewController : ReactiveViewController<SelectBeginningOfWeekViewModel>, IDismissableViewController
    {
        public SelectBeginningOfWeekViewController()
            : base(nameof(SelectBeginningOfWeekViewController))
        {
        }

        public async Task<bool> Dismiss()
        {
            ViewModel.Close.Execute();
            return true;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            TitleLabel.Text = Resources.FirstDayOfTheWeek;

            DaysTableView.RegisterNibForCellReuse(DayOfWeekViewCell.Nib, DayOfWeekViewCell.Identifier);

            var source = new CustomTableViewSource<SectionModel<Unit, SelectableBeginningOfWeekViewModel>, Unit, SelectableBeginningOfWeekViewModel>(
                DayOfWeekViewCell.CellConfiguration(DayOfWeekViewCell.Identifier),
                ViewModel.BeginningOfWeekCollection
            );

            source.Rx().ModelSelected()
                .Subscribe(ViewModel.SelectBeginningOfWeek.Inputs)
                .DisposedBy(DisposeBag);

            DaysTableView.Source = source;

            BackButton.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);
        }
    }
}

