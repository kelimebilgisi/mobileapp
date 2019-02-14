using System;
using Foundation;
using Toggl.Foundation.Extensions;
using Toggl.Multivac;
using UIKit;

namespace Toggl.Daneel.Cells.Settings
{
    public sealed partial class UpcomingEventsOptionCell : BaseTableViewCell<CalendarNotificationsOption>
    {
        public static readonly string Identifier = nameof(UpcomingEventsOptionCell);
        public static readonly NSString Key = new NSString(nameof(UpcomingEventsOptionCell));
        public static readonly UINib Nib;

        static UpcomingEventsOptionCell()
        {
            Nib = UINib.FromName(nameof(UpcomingEventsOptionCell), NSBundle.MainBundle);
        }

        public UpcomingEventsOptionCell(IntPtr handle) : base(handle)
        {
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();
            SelectionStyle = UITableViewCellSelectionStyle.None;
        }

        public override void SetSelected(bool selected, bool animated)
        {
            base.SetSelected(selected, animated);
            SelectedImageView.Hidden = !selected;
        }

        protected override void UpdateView()
        {
            TitleLabel.Text = Item.Title();
        }
    }
}
