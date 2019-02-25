using System.Reactive;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;
using Toggl.Daneel.Extensions;
using System.Threading.Tasks;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Views.CountrySelection;
using Toggl.Foundation;
using Toggl.Multivac.Extensions;
using Toggl.Daneel.ViewSources.Generic.TableView;
using Toggl.Foundation.MvvmCross.Collections;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public sealed partial class wSelectCountryViewController : KeyboardAwareViewController<SelectCountryViewModel>, IDismissableViewController
    {
        private const int rowHeight = 48;

        public SelectCountryViewController() : base(nameof(SelectCountryViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            TitleLabel.Text = Resources.CountryOfResidence;
            SearchTextField.Placeholder = Resources.Search;

            CountriesTableView.SeparatorStyle = UITableViewCellSeparatorStyle.None;
            CountriesTableView.RegisterNibForCellReuse(CountryViewCell.Nib, CountryViewCell.Identifier);
            CountriesTableView.RowHeight = rowHeight;

            var source = new CustomTableViewSource<SectionModel<string, SelectableCountryViewModel>, string, SelectableCountryViewModel>(
                CountryViewCell.CellConfiguration(CountryViewCell.Identifier));

            CountriesTableView.Source = source;

            source.Rx().ModelSelected()
                .Subscribe(ViewModel.SelectCountry.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.Countries
                .Subscribe(CountriesTableView.Rx().ReloadItems(source))
                .DisposedBy(DisposeBag);

            CloseButton.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);

            SearchTextField.Rx().Text()
                .Subscribe(ViewModel.FilterText)
                .DisposedBy(DisposeBag);



            SearchTextField.BecomeFirstResponder();
        }

        public async Task<bool> Dismiss()
        {
            ViewModel.Close.Execute(Unit.Default);
            return true;
        }

        protected override void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            BottomConstraint.AnimateSetConstant(e.FrameEnd.Height, View);
        }

        protected override void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            BottomConstraint.AnimateSetConstant(0, View);
        }
    }
}

