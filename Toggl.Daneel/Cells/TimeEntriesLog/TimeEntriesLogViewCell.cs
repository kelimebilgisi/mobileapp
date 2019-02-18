using System;
using System.Reactive;
using System.Reactive.Disposables;
using Foundation;
using MvvmCross.Plugin.Color.Platforms.Ios;
using MvvmCross.UI;
using Toggl.Daneel.Cells;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Transformations;
using Toggl.Foundation.MvvmCross.Helper;
using UIKit;
using Toggl.Foundation;
using Toggl.Foundation.MvvmCross.ViewModels.TimeEntriesLog;

namespace Toggl.Daneel.Views
{
    public partial class TimeEntriesLogViewCell : BaseTableViewCell<LogItemViewModel>
    {
        public static readonly string Identifier = "timeEntryCell";

        private ProjectTaskClientToAttributedString projectTaskClientToAttributedString;

        public static readonly NSString Key = new NSString(nameof(TimeEntriesLogViewCell));
        public static readonly UINib Nib;

        public CompositeDisposable DisposeBag = new CompositeDisposable();

        public IObservable<Unit> ContinueButtonTap
            => ContinueButton.Rx().Tap();

        public IObservable<Unit> ToggleGroup
            => GroupSizeContainer.Rx().Tap();

        static TimeEntriesLogViewCell()
        {
            Nib = UINib.FromName(nameof(TimeEntriesLogViewCell), NSBundle.MainBundle);
        }

        protected TimeEntriesLogViewCell(IntPtr handle)
            : base(handle)
        {
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            FadeView.FadeRight = true;

            TimeLabel.Font = TimeLabel.Font.GetMonospacedDigitFont();

            projectTaskClientToAttributedString = new ProjectTaskClientToAttributedString(
                ProjectTaskClientLabel.Font.CapHeight,
                Color.TimeEntriesLog.ClientColor.ToNativeColor(),
                true
            );
        }

        public override void PrepareForReuse()
        {
            base.PrepareForReuse();
            DisposeBag.Dispose();
            DisposeBag = new CompositeDisposable();

            BackgroundColor = UIColor.White;
            GroupSizeBackground.Layer.CornerRadius = 14;
        }

        protected override void UpdateView()
        {
            // Text
            var projectColor = MvxColor.ParseHexString(Item.ProjectColor).ToNativeColor();
            DescriptionLabel.Text = Item.HasDescription ? Item.Description : Resources.AddDescription;
            ProjectTaskClientLabel.AttributedText = projectTaskClientToAttributedString.Convert(Item.ProjectName, Item.TaskName, Item.ClientName, projectColor);
            TimeLabel.Text = Item.Duration;

            // Colors
            DescriptionLabel.TextColor = Item.HasDescription
                ? UIColor.Black
                : Color.TimeEntriesLog.AddDescriptionTextColor.ToNativeColor();

            // Visibility
            ProjectTaskClientLabel.Hidden = !Item.HasProject;
            SyncErrorImageView.Hidden = Item.CanContinue;
            UnsyncedImageView.Hidden = !Item.NeedsSync;
            ContinueButton.Hidden = !Item.CanContinue;
            ContinueImageView.Hidden = !Item.CanContinue;
            BillableIcon.Hidden = !Item.IsBillable;
            TagIcon.Hidden = !Item.HasTags;

            switch (Item.VisualizationIntent)
            {
                case LogItemVisualizationIntent.SingleItem:
                    presentAsSingleTimeEntry();
                    break;

                case LogItemVisualizationIntent.GroupItem:
                    presentAsTimeEntryInAGroup();
                    break;

                case LogItemVisualizationIntent.CollapsedGroupHeader:
                    presentAsCollapsedGroupHeader(Item.RepresentedTimeEntriesIds.Length);
                    break;

                case LogItemVisualizationIntent.ExpandedGroupHeader:
                    presentAsExpandedGroupHeader(Item.RepresentedTimeEntriesIds.Length);
                    break;

                default:
                    throw new ArgumentOutOfRangeException($"Cannot visualize {Item.VisualizationIntent} in the time entries log table.");
            }
        }

        protected override void Dispose(bool disposing)
        {
            DisposeBag.Dispose();
            base.Dispose(disposing);
        }

        private void presentAsCollapsedGroupHeader(int groupSize)
        {
            GroupSizeLabel.Text = groupSize.ToString();
            GroupSizeContainer.Hidden = false;
            GroupSizeContainer.UserInteractionEnabled = true;
            GroupSizeBackground.Hidden = false;
            GroupSizeBackground.Layer.BorderWidth = 1;
            GroupSizeBackground.Layer.BorderColor = Color.TimeEntriesLog.Grouping.Collapsed.Border.ToNativeColor().CGColor;
            GroupSizeBackground.BackgroundColor = Color.TimeEntriesLog.Grouping.Collapsed.Background.ToNativeColor();
            GroupSizeLabel.TextColor = Color.TimeEntriesLog.Grouping.Collapsed.Text.ToNativeColor();
        }

        private void presentAsExpandedGroupHeader(int groupSize)
        {
            GroupSizeLabel.Text = groupSize.ToString();
            GroupSizeContainer.Hidden = false;
            GroupSizeContainer.UserInteractionEnabled = true;
            GroupSizeBackground.Hidden = false;
            GroupSizeBackground.Layer.BorderWidth = 0;
            GroupSizeBackground.BackgroundColor = Color.TimeEntriesLog.Grouping.Expanded.Background.ToNativeColor();
            GroupSizeLabel.TextColor = Color.TimeEntriesLog.Grouping.Expanded.Text.ToNativeColor();
        }

        private void presentAsSingleTimeEntry()
        {
            GroupSizeContainer.Hidden = true;
        }

        private void presentAsTimeEntryInAGroup()
        {
            GroupSizeContainer.Hidden = false;
            GroupSizeContainer.UserInteractionEnabled = false;
            GroupSizeBackground.Hidden = true;
            BackgroundColor = Color.TimeEntriesLog.Grouping.GroupedTimeEntry.Background.ToNativeColor();
        }
    }
}
