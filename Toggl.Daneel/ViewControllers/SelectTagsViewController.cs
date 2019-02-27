using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;
using Toggl.Foundation.MvvmCross.Helper;
using System.Threading.Tasks;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Views.Tag;
using Toggl.Daneel.ViewSources;
using Toggl.Multivac.Extensions;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public sealed partial class SelectTagsViewController : KeyboardAwareViewController<SelectTagsViewModel>, IDismissableViewController
    {
        public SelectTagsViewController()
            : base(nameof(SelectTagsViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var tableViewSource = new SelectTagsTableViewSource(TagsTableView);

            tableViewSource.Rx().ModelSelected()
                .Subscribe(ViewModel.SelectTag.Inputs)
                .DisposedBy(DisposeBag);

            ViewModel.Tags
                .Subscribe(TagsTableView.Rx().ReloadItems(tableViewSource))
                .DisposedBy(DisposeBag);

            ViewModel.IsEmpty
                .Subscribe(EmptyStateImage.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsEmpty
                .Subscribe(EmptyStateLabel.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.FilterText
                .Subscribe(TextField.Rx().TextObserver())
                .DisposedBy(DisposeBag);

            CloseButton.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);

            SaveButton.Rx()
                .BindAction(ViewModel.Save)
                .DisposedBy(DisposeBag);

            TextField.Rx().Text()
                .Subscribe(ViewModel.FilterText)
                .DisposedBy(DisposeBag);
        }

        public override void ViewWillAppear(bool animated)
        {
            base.ViewWillAppear(animated);
            TextField.BecomeFirstResponder();
        }

        public async Task<bool> Dismiss()
        {
            await ViewModel.Close.Execute();
            return true;
        }

        protected override void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            BottomConstraint.Constant = e.FrameEnd.Height;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        protected override void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            BottomConstraint.Constant = 0;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }
    }
}
