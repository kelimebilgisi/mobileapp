using System;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding;
using MvvmCross.Platforms.Ios.Binding.Views;
using MvvmCross.Plugin.Color.Platforms.Ios;
using MvvmCross.Plugin.Visibility;
using MvvmCross.UI;
using Toggl.Daneel.Cells;
using Toggl.Daneel.Combiners;
using Toggl.Daneel.Transformations;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.Helper;
using UIKit;

namespace Toggl.Daneel.Views
{
    public partial class StartTimeEntryViewCell : BaseTableViewCell<TimeEntrySuggestion>
    {
        private const float NoProjectDistance = 16;
        private const float HasProjectDistance = 8;

        private ProjectTaskClientToAttributedString projectTaksClientToAttributedString;

        public static readonly string Identifier = nameof(StartTimeEntryViewCell);
        public static readonly UINib Nib;

        static StartTimeEntryViewCell()
        {
            Nib = UINib.FromName(nameof(StartTimeEntryViewCell), NSBundle.MainBundle);
        }

        protected StartTimeEntryViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            projectTaksClientToAttributedString = new ProjectTaskClientToAttributedString(
                ProjectLabel.Font.CapHeight,
                Color.Suggestions.ClientColor.ToNativeColor(),
                true);
        }

        protected override void UpdateView()
        {
            //Text
            DescriptionLabel.Text = Item.Description;
            ProjectLabel.AttributedText = projectTaksClientToAttributedString.Convert(
                Item.ProjectName,
                Item.TaskName,
                Item.ClientName,
                MvxColor.ParseHexString(Item.ProjectColor).ToNativeColor());

            //Visibility
            DescriptionTopDistanceConstraint.Constant = Item.HasProject ? HasProjectDistance : NoProjectDistance;
            ProjectLabel.Hidden = !Item.HasProject;
        }
    }
}
