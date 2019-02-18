using System;
using Foundation;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Platforms.Ios.Binding.Views;
using Toggl.Daneel.Cells;
using Toggl.Foundation.MvvmCross.Collections;
using Toggl.Daneel.Views.Interfaces;
using UIKit;

namespace Toggl.Daneel.Views
{
    public sealed partial class WorkspaceHeaderViewCell : BaseTableHeaderFooterView<string>, IHeaderViewCellWithHideableTopSeparator
    {
        public static readonly string Identifier = nameof(WorkspaceHeaderViewCell);
        public static readonly UINib Nib;

        public bool TopSeparatorHidden
        {
            get => TopSeparator.Hidden;
            set => TopSeparator.Hidden = value;
        }

        static WorkspaceHeaderViewCell()
        {
            Nib = UINib.FromName(nameof(WorkspaceHeaderViewCell), NSBundle.MainBundle);
        }

        public WorkspaceHeaderViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        protected override void UpdateView()
        {
            WorkspaceLabel.Text = Item;
        }
    }
}
