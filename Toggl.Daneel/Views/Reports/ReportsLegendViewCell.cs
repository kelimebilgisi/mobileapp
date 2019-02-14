using System;
using Foundation;
using MvvmCross.Plugin.Color.Platforms.Ios;
using MvvmCross.UI;
using Toggl.Daneel.Cells;
using Toggl.Daneel.Extensions;
using Toggl.Foundation.Extensions;
using Toggl.Foundation.Reports;
using UIKit;

namespace Toggl.Daneel.Views.Reports
{
    public partial class ReportsLegendViewCell : BaseTableViewCell<ChartSegment>
    {
        public static readonly string Identifier = nameof(ReportsLegendViewCell);
        public static readonly NSString Key = new NSString(nameof(ReportsLegendViewCell));
        public static readonly UINib Nib;

        static ReportsLegendViewCell()
        {
            Nib = UINib.FromName(nameof(ReportsLegendViewCell), NSBundle.MainBundle);
        }

        protected ReportsLegendViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            FadeView.FadeRight = true;
        }

        protected override void UpdateView()
        {
            ProjectLabel.SetKerning(-0.2);
            ClientLabel.SetKerning(-0.2);
            TotalTimeLabel.SetKerning(-0.2);
            PercentageLabel.SetKerning(-0.2);

            //Text
            ProjectLabel.Text = Item.ProjectName;
            ClientLabel.Text = Item.ClientName;
            PercentageLabel.Text = $"{Item.Percentage:P2}";
            TotalTimeLabel.Text = Item.TrackedTime.ToFormattedString(Item.DurationFormat);

            ClientLabel.Hidden = !Item.HasClient;

            // Color
            var color = MvxColor.ParseHexString(Item.Color).ToNativeColor();
            ProjectLabel.TextColor = color;
            CircleView.BackgroundColor = color;
        }
    }
}
