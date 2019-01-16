using System;
using Foundation;
using UIKit;

namespace Toggl.Daneel.Views.StartTimeEntry
{
    public sealed partial class ReactiveWorkspaceHeaderViewCell : UITableViewHeaderFooterView
    {
        public static readonly NSString Key = new NSString(nameof(ReactiveWorkspaceHeaderViewCell));
        public static readonly UINib Nib;

        public bool TopSeparatorHidden
        {
            get => TopSeparator.Hidden;
            set => TopSeparator.Hidden = value;
        }

        private string workspaceName = "";
        public string WorkspaceName
        {
            get => workspaceName;
            set
            {
                workspaceName = value;
                updateView();
            }
        }

        static ReactiveWorkspaceHeaderViewCell()
        {
            Nib = UINib.FromName(nameof(ReactiveWorkspaceHeaderViewCell), NSBundle.MainBundle);
        }

        protected ReactiveWorkspaceHeaderViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        private void updateView()
        {
            WorkspaceNameLabel.Text = WorkspaceName;
        }
    }
}

