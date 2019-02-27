using System;

using Foundation;
using MvvmCross.Plugin.Color.Platforms.Ios;
using Toggl.Daneel.Cells;
using Toggl.Daneel.Extensions;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.Views.Tag
{
    public partial class NewTagViewCell : BaseTableViewCell<SelectableTagBaseViewModel>
    {
        public static readonly string Identifier  = nameof(NewTagViewCell);
        public static readonly UINib Nib;

        private static readonly UIColor selectedBackgroundColor
            = Color.Common.LightGray.ToNativeColor();
        private static UIImage checkBoxCheckedImage = UIImage.FromBundle("icCheckBoxChecked");
        private static UIImage checkBoxUncheckedImage = UIImage.FromBundle("icCheckBoxUnchecked");

        static NewTagViewCell()
        {
            Nib = UINib.FromName(nameof(NewTagViewCell), NSBundle.MainBundle);
        }

        protected NewTagViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void SetSelected(bool selected, bool animated)
            => setBackgroundColor(selected, animated);

        public override void SetHighlighted(bool highlighted, bool animated)
            => setBackgroundColor(highlighted, animated);

        protected override void UpdateView()
        {
            TextLabel.Text = Item.Name;
            SelectedImageView.Image = Item.Selected ? checkBoxCheckedImage : checkBoxUncheckedImage;
        }

        private void setBackgroundColor(bool selected, bool animated)
        {
            var targetColor = selected ? selectedBackgroundColor : UIColor.White;

            if (animated)
                animateBackgroundColor(targetColor);
            else
                BackgroundColor = targetColor;
        }

        private void animateBackgroundColor(UIColor color)
        {
            AnimationExtensions.Animate(
                Animation.Timings.EnterTiming,
                Animation.Curves.EaseIn,
                () => BackgroundColor = color
            );
        }
    }
}

