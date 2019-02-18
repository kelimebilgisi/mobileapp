using System;
using Foundation;
using Toggl.Daneel.Cells;
using Toggl.Daneel.Extensions;
using Toggl.Foundation.Autocomplete.Suggestions;
using UIKit;

namespace Toggl.Daneel.Views.StartTimeEntry
{
    public sealed partial class NoEntityInfoViewCell : BaseTableViewCell<NoEntityInfoMessage>
    {
        public static readonly string Identifier = nameof(NoEntityInfoViewCell);
        public static readonly UINib Nib;

        static NoEntityInfoViewCell()
        {
            Nib = UINib.FromName(nameof(NoEntityInfoViewCell), NSBundle.MainBundle);
        }

        protected NoEntityInfoViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        protected override void UpdateView()
        {
            Label.AttributedText = Item.ToAttributedString(Label.Font.CapHeight);
        }
    }
}
