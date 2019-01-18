using System;
using Foundation;
using Toggl.Daneel.Cells;
using Toggl.Foundation.Autocomplete.Suggestions;
using UIKit;

namespace Toggl.Daneel.Views.StartTimeEntry
{
    public sealed partial class ReactiveTaskSuggestionViewCell : BaseTableViewCell<TaskSuggestion>
    {
        public static readonly NSString Key = new NSString(nameof(ReactiveTaskSuggestionViewCell));
        public static readonly UINib Nib;

        static ReactiveTaskSuggestionViewCell()
        {
            Nib = UINib.FromName(nameof(ReactiveTaskSuggestionViewCell), NSBundle.MainBundle);
        }

        protected ReactiveTaskSuggestionViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            FadeView.FadeRight = true;
        }

        protected override void UpdateView()
        {
            TaskNameLabel.Text = Item.Name;
        }
    }
}