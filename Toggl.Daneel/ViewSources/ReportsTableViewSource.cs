using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CoreGraphics;
using Foundation;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Views.Reports;
using Toggl.Daneel.ViewSources.Generic.TableView;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels.Reports;
using Toggl.Foundation.Reports;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewSources
{
    public sealed class ReportsTableViewSource : BaseTableViewSource<ReportsViewModel, ChartSegment>
    {
        private const int summaryHeight = 469;

        private readonly CompositeDisposable disposeBag = new CompositeDisposable();
        private readonly ReportsViewModel viewModel;
        private readonly UITableView tableView;

        public IObservable<CGPoint> ScrolledWithHeaderOffset { get; }

        private readonly nfloat headerHeight;
        private readonly nfloat bottomHeight = 78;
        private readonly nfloat rowHeight = 56;

        private readonly CellConfiguration<ChartSegment> cellConfiguration =
            ReportsLegendViewCell.CellConfiguration(ReportsLegendViewCell.Identifier);

        public ReportsTableViewSource(UITableView tableView, ReportsViewModel viewModel)
        {
            this.viewModel = viewModel;
            this.tableView = tableView;

            headerHeight = summaryHeight + UIScreen.MainScreen.Bounds.Width;
            tableView.TableHeaderView = new UIView(new CGRect(0, 0, tableView.Bounds.Size.Width, headerHeight));
            tableView.ContentInset = new UIEdgeInsets(-headerHeight, 0,  0, 0);
            tableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
            tableView.RowHeight = rowHeight;
            tableView.SectionHeaderHeight = headerHeight;
            tableView.RegisterNibForCellReuse(ReportsLegendViewCell.Nib, ReportsLegendViewCell.Identifier);
            tableView.RegisterNibForHeaderFooterViewReuse(ReportsHeaderView.Nib, ReportsHeaderView.Identifier);
            tableView.BackgroundColor = Color.Reports.Background.ToNativeColor();

            this.viewModel.WorkspacesObservable
                .Select(workspaces => workspaces.Count)
                .Subscribe(updateWorkspaceCount)
                .DisposedBy(disposeBag);

            ScrolledWithHeaderOffset = this.Rx().Scrolled()
                .Select(offset => new CGPoint(offset.X, offset.Y - headerHeight));
        }

        private void updateWorkspaceCount(int workspaceCount)
        {
            tableView.ContentInset = new UIEdgeInsets(-headerHeight, 0, workspaceCount > 1 ? bottomHeight : 0, 0);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            disposeBag.Dispose();
        }

        public override UIView GetViewForHeader(UITableView tableView, nint section)
        {
            var header = tableView.DequeueReusableHeaderFooterView(ReportsHeaderView.Identifier) as ReportsHeaderView;
            header.Item = viewModel;
            return header;
        }

        public override nfloat GetHeightForHeader(UITableView tableView, nint section)
        {
            return headerHeight;
        }

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
            => cellConfiguration(tableView, indexPath, ModelAt(indexPath));
    }
}
