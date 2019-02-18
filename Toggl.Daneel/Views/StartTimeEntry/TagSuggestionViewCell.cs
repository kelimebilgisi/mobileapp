using System;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding.Views;
using Toggl.Daneel.Cells;
using Toggl.Foundation.Autocomplete.Suggestions;
using UIKit;

namespace Toggl.Daneel.Views
{
    public sealed partial class TagSuggestionViewCell : BaseTableViewCell<TagSuggestion>
    {
        public static readonly string Identifier = nameof(TagSuggestionViewCell);
        public static readonly UINib Nib;

        static TagSuggestionViewCell()
        {
            Nib = UINib.FromName(nameof(TagSuggestionViewCell), NSBundle.MainBundle);
        }

        public TagSuggestionViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        protected override void UpdateView()
        {
            NameLabel.Text = Item.Name;
        }
    }
}
