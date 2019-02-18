using System;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Commands;
using MvvmCross.Platforms.Ios.Binding;
using MvvmCross.Platforms.Ios.Binding.Views;
using MvvmCross.Plugin.Color;
using MvvmCross.Plugin.Color.Platforms.Ios;
using MvvmCross.Plugin.Visibility;
using MvvmCross.UI;
using Toggl.Daneel.Cells;
using Toggl.Daneel.Combiners;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.MvvmCross.Converters;
using UIKit;

namespace Toggl.Daneel.Views
{
    public partial class ProjectSuggestionViewCell : BaseTableViewCell<ProjectSuggestion>
    {
        private const float selectedProjectBackgroundAlpha = 0.12f;

        private const int fadeViewTrailingConstraintWithTasks = 72;
        private const int fadeViewTrailingConstraintWithoutTasks = 16;

        public static readonly string Identifier = nameof(ProjectSuggestionViewCell);
        public static readonly UINib Nib;

        static ProjectSuggestionViewCell()
        {
            Nib = UINib.FromName(nameof(ProjectSuggestionViewCell), NSBundle.MainBundle);
        }

        protected ProjectSuggestionViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public IMvxCommand<ProjectSuggestion> ToggleTasksCommand { get; set; }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            FadeView.FadeRight = true;
            ClientNameLabel.LineBreakMode = UILineBreakMode.TailTruncation;
            ProjectNameLabel.LineBreakMode = UILineBreakMode.TailTruncation;
            ToggleTasksButton.TouchUpInside += togglTasksButton;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;
            ToggleTasksButton.TouchUpInside -= togglTasksButton;
        }

        private void togglTasksButton(object sender, EventArgs e)
            => ToggleTasksCommand?.Execute(Item);

        protected override void UpdateView()
        {
            //Text
            ProjectNameLabel.Text = Item.ProjectName;
            ClientNameLabel.Text = Item.ClientName;
            var optionalS = Item.NumberOfTasks == 1 ? "" : "s";
            AmountOfTasksLabel.Text = Item.NumberOfTasks == 0 ? "" : $"{Item.NumberOfTasks} Task{optionalS}";

            //Color
            var projectColor = MvxColor.ParseHexString(Item.ProjectColor).ToNativeColor();
            ProjectNameLabel.TextColor = projectColor;
            ProjectDotView.BackgroundColor = projectColor;
            SelectedProjectView.BackgroundColor = Item.Selected
                ? projectColor.ColorWithAlpha(selectedProjectBackgroundAlpha)
                : UIColor.Clear;

            //Visibility
            ToggleTaskImage.Hidden = !Item.HasTasks;
            ToggleTasksButton.Hidden = !Item.HasTasks;

            //Constraints
            FadeViewTrailingConstraint.Constant = Item.HasTasks
                ? fadeViewTrailingConstraintWithTasks
                : fadeViewTrailingConstraintWithoutTasks;
        }
    }
}
