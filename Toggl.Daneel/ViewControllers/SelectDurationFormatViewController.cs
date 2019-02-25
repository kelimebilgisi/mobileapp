using System.Reactive;
using System.Reactive.Disposables;
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
    public partial class SelectDurationFormatViewController : ReactiveViewController<SelectDurationFormatViewModel>, IDismissableViewController
    {
        private const int rowHeight = 48;

        CompositeDisposable disposeBag = new CompositeDisposable();

        public SelectDurationFormatViewController()
            : base(nameof(SelectDurationFormatViewController))
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

            TitleLabel.Text = Resources.DurationFormat;

            DurationFormatsTableView.RowHeight = rowHeight;
            DurationFormatsTableView.RegisterNibForCellReuse(DurationFormatViewCell.Nib, DurationFormatViewCell.Identifier);

            var source = new CustomTableViewSource<SectionModel<Unit, SelectableDurationFormatViewModel>, Unit, SelectableDurationFormatViewModel>(
                DurationFormatViewCell.CellConfiguration(DurationFormatViewCell.Identifier),
                ViewModel.DurationFormats
            );

            DurationFormatsTableView.Source = source;

            source.Rx().ModelSelected()
                .Subscribe(ViewModel.SelectDurationFormat.Inputs)
                .DisposedBy(disposeBag);

            BackButton.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(disposeBag);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            disposeBag.Dispose();
        }
    }
}
