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
            source.ToggleTaskSuggestions = ViewModel.ToggleTaskSuggestions;
            source.SelectProject = ViewModel.SelectProject;
            source.UseGrouping = ViewModel.UseGrouping;
            ProjectsTableView.TableFooterView = new UIView();
            ProjectsTableView.RegisterNibForCellReuse(ReactiveProjectSuggestionViewCell.Nib, ReactiveProjectSuggestionViewCell.Key);
            ProjectsTableView.RegisterNibForCellReuse(ReactiveTaskSuggestionViewCell.Nib, ReactiveTaskSuggestionViewCell.Key);
            ProjectsTableView.RegisterNibForHeaderFooterViewReuse(ReactiveWorkspaceHeaderViewCell.Nib, ReactiveWorkspaceHeaderViewCell.Key);
            ProjectsTableView.Rx()
                .Bind(source)
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

            TextField.Rx().Text()
                .Subscribe(ViewModel.FilterText)
                .DisposedBy(DisposeBag);

            CloseButton.Rx()
                .BindAction(ViewModel.Close)
                .DisposedBy(DisposeBag);

            source.ItemSelected.Subscribe(ViewModel.SelectProject.Inputs);

            //var source = new SelectProjectTableViewSourceOld(ProjectsTableView);
            //ProjectsTableView.Source = source;
            //source.ToggleTasksCommand = new MvxCommand<ProjectSuggestion>(toggleTaskSuggestions);

            ////Table view
            //bindingSet.Bind(source)
            //          .For(v => v.ObservableCollection)
            //          .To(vm => vm.Suggestions);

            //bindingSet.Bind(source)
            //          .For(v => v.CreateCommand)
            //          .To(vm => vm.CreateProjectCommand);

            //bindingSet.Bind(source)
            //          .For(v => v.SuggestCreation)
            //          .To(vm => vm.SuggestCreation);

            //bindingSet.Bind(source)
            //          .For(v => v.UseGrouping)
            //          .To(vm => vm.UseGrouping);

            //bindingSet.Bind(source)
            //          .For(v => v.Text)
            //          .To(vm => vm.Text);

            ////Text
            //bindingSet.Bind(TextField).To(vm => vm.Text);

            ////Commands
            //bindingSet.Bind(source)
            //.For(s => s.SelectionChangedCommand)
            //.To(vm => vm.SelectProjectCommand);

            //bindingSet.Apply();

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
