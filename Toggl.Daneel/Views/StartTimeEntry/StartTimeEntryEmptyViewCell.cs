using System;
using System.Globalization;
using Accelerate;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding.Views;
using MvvmCross.Converters;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Cells;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.MvvmCross.Helper;
using UIKit;

namespace Toggl.Daneel.Views
{
    public partial class StartTimeEntryEmptyViewCell : BaseTableViewCell<QuerySymbolSuggestion>
    {
        public static readonly string Identifier = nameof(StartTimeEntryEmptyViewCell);
        public static readonly UINib Nib;

        static StartTimeEntryEmptyViewCell()
        {
            Nib = UINib.FromName(nameof(StartTimeEntryEmptyViewCell), NSBundle.MainBundle);
        }

        protected StartTimeEntryEmptyViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        protected override void UpdateView()
        {
            DescriptionLabel.AttributedText = suggestionToAttributedString(Item);
        }

        private NSAttributedString suggestionToAttributedString(QuerySymbolSuggestion value)
        {
            var a = $"{value.Symbol} {value.Description}";
            var result = new NSMutableAttributedString(a);
            result.AddAttributes(new UIStringAttributes
            {
                Font = UIFont.BoldSystemFontOfSize(16),
                ForegroundColor = Color.StartTimeEntry.BoldQuerySuggestionColor.ToNativeColor()
            }, new NSRange(0, 1));

            result.AddAttributes(new UIStringAttributes
            {
                Font = UIFont.SystemFontOfSize(13),
                ForegroundColor = Color.StartTimeEntry.Placeholder.ToNativeColor()
            }, new NSRange(2, value.Description.Length));

            return result;
        }
    }
}
