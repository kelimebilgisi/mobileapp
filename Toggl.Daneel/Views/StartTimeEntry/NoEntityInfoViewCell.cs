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
            Label.AttributedText = toAttributedString(Item, Label.Font.CapHeight);
        }

        private NSAttributedString toAttributedString(NoEntityInfoMessage noEntityInfoMessage, nfloat fontCapHeight)
        {
            if (string.IsNullOrEmpty(noEntityInfoMessage.ImageResource))
                return new NSAttributedString(noEntityInfoMessage.Text);

            var result = new NSMutableAttributedString(noEntityInfoMessage.Text);
            var rangeToBeReplaced = new NSRange(
                noEntityInfoMessage
                    .Text
                    .IndexOf(noEntityInfoMessage.CharacterToReplace.Value),
                len: 1);
            var imageAttachment = noEntityInfoMessage
                .ImageResource
                .GetAttachmentString(
                    fontCapHeight,
                    UIImageRenderingMode.AlwaysOriginal);

            result.Replace(rangeToBeReplaced, imageAttachment);

            return result;
        }
    }
}
