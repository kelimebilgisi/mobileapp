using System;
using Foundation;
using Toggl.Daneel.Cells;
using Toggl.Daneel.Extensions;
using UIKit;

namespace Toggl.Daneel.Views.EntityCreation
{
    public sealed partial class CreateEntityViewcell : BaseTableViewCell<string>
    {
        public static readonly NSString Key = new NSString(nameof(CreateEntityViewcell));
        public static readonly UINib Nib;

        static CreateEntityViewcell()
        {
            Nib = UINib.FromName(nameof(CreateEntityViewcell), NSBundle.MainBundle);
        }

        protected CreateEntityViewcell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        protected override void UpdateView()
        {
            TextLabel.AttributedText = Item.PrependWithAddIcon(TextLabel.Font.CapHeight);
        }
    }
}

