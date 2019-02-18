using System;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding.Views;
using Toggl.Daneel.Cells;
using Toggl.Foundation.Autocomplete.Suggestions;
using UIKit;

namespace Toggl.Daneel.Views
{
    public sealed partial class TaskSuggestionViewCell : BaseTableViewCell<TaskSuggestion>
    {
        public static readonly string Identifier = nameof(TaskSuggestionViewCell);
        public static readonly UINib Nib;

        static TaskSuggestionViewCell()
        {
            Nib = UINib.FromName(nameof(TaskSuggestionViewCell), NSBundle.MainBundle);
        }

        public TaskSuggestionViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            FadeView.FadeRight = true;
        }

        protected override void UpdateView()
        {
            TaskNameLabel.Text = Item.Name;
        }
    }
}
