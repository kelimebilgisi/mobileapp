using System;
using Foundation;
using Toggl.Daneel.Cells;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.Views.Settings
{
    public partial class DayOfWeekViewCell : BaseTableViewCell<SelectableBeginningOfWeekViewModel>
    {
        public static readonly string Identifier = nameof(DayOfWeekViewCell);
        public static readonly NSString Key = new NSString(nameof(DayOfWeekViewCell));
        public static readonly UINib Nib;

        static DayOfWeekViewCell()
        {
            Nib = UINib.FromName(nameof(DayOfWeekViewCell), NSBundle.MainBundle);
        }

        protected DayOfWeekViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        protected override void UpdateView()
        {
            DayOfWeekLabel.Text = Item.BeginningOfWeek.ToString();
            SelectedImageView.Hidden = !Item.Selected;
        }
    }
}
