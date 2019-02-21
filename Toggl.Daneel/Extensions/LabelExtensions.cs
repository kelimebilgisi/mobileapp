using Foundation;
using UIKit;

namespace Toggl.Daneel.Extensions
{
    public static class LabelExtensions
    {
        public static void SetKerning(this UILabel label, double letterSpacing)
        {
            var text = label.Text ?? "";
            var attributedText = new NSMutableAttributedString(text);
            var range = new NSRange(0, text.Length);

            attributedText.AddAttribute(UIStringAttributeKey.KerningAdjustment, new NSNumber(letterSpacing), range);

            label.AttributedText = attributedText;
        }
    }
}
