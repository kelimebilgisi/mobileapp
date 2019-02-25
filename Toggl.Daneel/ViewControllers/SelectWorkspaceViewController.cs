using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.Views;
using Toggl.Daneel.ViewSources.Generic.TableView;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public partial class SelectWorkspaceViewController : ReactiveViewController<SelectWorkspaceViewModel>, IDismissableViewController
    {
        private const int rowHeight = 64;

        public SelectWorkspaceViewController()
            : base(nameof(SelectWorkspaceViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            WorkspaceTableView.RowHeight = rowHeight;
            WorkspaceTableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
            WorkspaceTableView.RegisterNibForCellReuse(WorkspaceViewCell.Nib, WorkspaceViewCell.Identifier);

            var source = new CustomTableViewSource<SectionModel<Unit, SelectableWorkspaceViewModel>, Unit, SelectableWorkspaceViewModel>(
                WorkspaceViewCell.CellConfiguration(WorkspaceViewCell.Identifier),
                ViewModel.Workspaces
            );
            WorkspaceTableView.Source = source;

            TitleLabel.Text = ViewModel.Title;

            CloseButton.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);

            source.Rx().ModelSelected()
                .Subscribe(ViewModel.SelectWorkspace.Inputs)
                .DisposedBy(DisposeBag);
        }

        public async Task<bool> Dismiss()
        {
            await ViewModel.Close.Execute();
            return true;
        }
    }
}

