using System.Threading.Tasks;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Daneel.Extensions.Reactive;
using UIKit;
using System;
using System.Reactive.Linq;
using static Toggl.Multivac.Extensions.ReactiveExtensions;
using Toggl.Daneel.Views.StartTimeEntry;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public sealed partial class SelectProjectViewController : KeyboardAwareViewController<SelectProjectViewModel>, IDismissableViewController
    {
        public SelectProjectViewController()
            : base(nameof(SelectProjectViewController))
        {
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            var source = new SelectProjectTableViewSource(ViewModel.Suggestions, ReactiveProjectSuggestionViewCell.Key);
            source.UseGrouping = ViewModel.UseGrouping;
            source.RegisterViewCells(ProjectsTableView);

            ProjectsTableView.TableFooterView = new UIView();

            ProjectsTableView.Rx()
                .Bind(source, ViewModel.CreateEntitySuggestion.Select(createEntitySuggestion => createEntitySuggestion != null))
                .DisposedBy(DisposeBag);

            ViewModel.IsEmpty
                .Subscribe(EmptyStateLabel.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.IsEmpty
                .Subscribe(EmptyStateImage.Rx().IsVisible())
                .DisposedBy(DisposeBag);

            ViewModel.PlaceholderText
                .Subscribe(TextField.Rx().PlaceholderText())
                .DisposedBy(DisposeBag);

            ViewModel.CreateEntitySuggestion
                .Subscribe(source.OnCreateEntitySuggestion)
                .DisposedBy(DisposeBag);

            TextField.Rx().Text()
                .Subscribe(ViewModel.FilterText)
                .DisposedBy(DisposeBag);

            CloseButton.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);

            source.ItemSelected
                .Subscribe(ViewModel.SelectProject.Inputs)
                .DisposedBy(DisposeBag);

            source.ToggleTaskSuggestion
                .Subscribe(ViewModel.ToggleTaskSuggestions.Inputs)
                .DisposedBy(DisposeBag);

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
