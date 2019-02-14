using System;
using System.Linq;
using MvvmCross.Platforms.Ios.Views;
using Toggl.Daneel.Cells.Settings;
using Toggl.Daneel.ViewSources;
using Toggl.Daneel.ViewSources.Generic.TableView;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    public sealed class LicensesViewController : MvxTableViewController<LicensesViewModel>
    {
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            Title = Resources.Licenses;

            TableView.EstimatedRowHeight = 396;
            TableView.SectionHeaderHeight = 44;
            TableView.RowHeight = UITableView.AutomaticDimension;

            TableView.RegisterNibForCellReuse(LicensesViewCell.Nib, LicensesViewCell.Identifier);
            TableView.RegisterNibForHeaderFooterViewReuse(LicensesHeaderViewCell.Nib,
                LicensesHeaderViewCell.Identifier);

            var sectionedLicenses = ViewModel.Licenses
                .Select(license => new CollectionSection<License, License>(license, new [] {license}));

            var source = new CustomTableViewSource<License, License>(
                LicensesViewCell.CellConfiguration(LicensesViewCell.Identifier),
                LicensesHeaderViewCell.HeaderConfiguration,
                sectionedLicenses
            );

            TableView.Source = source;
        }
    }
}
