using System;
using Foundation;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Cells;
using Toggl.Daneel.Extensions;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.MvvmCross.Helper;
using UIKit;

namespace Toggl.Daneel.Views.EntityCreation
{
    public sealed partial class CreateEntityViewCell : BaseTableViewCell<CreateEntitySuggestion>
    {
        public static readonly string Identifier = nameof(CreateEntityViewCell);
        public static readonly UINib Nib;

        private NSAttributedString cachedAddIcon;

        static CreateEntityViewCell()
        {
            Nib = UINib.FromName(nameof(CreateEntityViewCell), NSBundle.MainBundle);
        }

        protected CreateEntityViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            cachedAddIcon = "".PrependWithAddIcon(TextLabel.Font.CapHeight);
        }

        protected override void UpdateView()
        {
            var result = new NSMutableAttributedString(cachedAddIcon);
            var text = new NSMutableAttributedString(Item.CreateEntityMessage);
            var textColor = Color.StartTimeEntry.Placeholder.ToNativeColor();

            text.AddAttribute(UIStringAttributeKey.ForegroundColor, textColor, new NSRange(0, text.Length));
            result.Append(text);

            TextLabel.AttributedText = result;
        }
    }
}
