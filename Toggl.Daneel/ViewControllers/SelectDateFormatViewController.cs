using System.Reactive.Disposables;
using System.Threading.Tasks;
using MvvmCross.Platforms.Ios.Views;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.Views.Settings;
using Toggl.Daneel.ViewSources.Generic.TableView;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Foundation.MvvmCross.ViewModels.Selectable;
using Toggl.Multivac.Extensions;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public sealed partial class SelectDateFormatViewController
        : MvxViewController<SelectDateFormatViewModel>,
          IDismissableViewController
    {
        private const int rowHeight = 48;

        private readonly CompositeDisposable disposeBag = new CompositeDisposable();

        public SelectDateFormatViewController() : base(nameof(SelectDateFormatViewController), null)
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            TitleLabel.Text = Resources.DateFormat;

            DateFormatsTableView.RegisterNibForCellReuse(DateFormatViewCell.Nib, DateFormatViewCell.Identifier);
            DateFormatsTableView.RowHeight = rowHeight;

            var source = new CustomTableViewSource<SectionModel<string, SelectableDateFormatViewModel>, string, SelectableDateFormatViewModel>(
                DateFormatViewCell.CellConfiguration(DateFormatViewCell.Identifier),
                ViewModel.DateTimeFormats
            );

            DateFormatsTableView.Source = source;

            source.Rx().ModelSelected()
                .Subscribe(ViewModel.SelectDateFormat.Inputs)
                .DisposedBy(disposeBag);

            BackButton.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(disposeBag);
        }

        public async Task<bool> Dismiss()
        {
            ViewModel.Close.Execute();
            return true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            disposeBag.Dispose();
        }
    }
}

