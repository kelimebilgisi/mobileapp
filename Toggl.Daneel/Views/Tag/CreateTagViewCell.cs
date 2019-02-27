using System;

using Foundation;
using Toggl.Daneel.Cells;
using Toggl.Foundation.MvvmCross.ViewModels;
using UIKit;

namespace Toggl.Daneel.Views.Tag
{
    public partial class CreateTagViewCell : BaseTableViewCell<SelectableTagBaseViewModel>
    {
        public static readonly string Identifier = nameof(CreateTagViewCell);
        public static readonly UINib Nib;

        static CreateTagViewCell()
        {
            Nib = UINib.FromName("CreateTagViewCell", NSBundle.MainBundle);
        }

        protected CreateTagViewCell(IntPtr handle) : base(handle)
        {
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();
            NameLabel.Text = string.Empty;
        }

        protected override void UpdateView()
        {
            NameLabel.Text = $"Create tag \"{Item.Name.Trim()}\"";
        }
    }
}

