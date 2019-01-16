// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Toggl.Daneel.Views.StartTimeEntry
{
	[Register ("ReactiveTaskSuggestionViewCell")]
	partial class ReactiveTaskSuggestionViewCell
	{
		[Outlet]
		Toggl.Daneel.Views.FadeView FadeView { get; set; }

		[Outlet]
		UIKit.UILabel TaskNameLabel { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (FadeView != null) {
				FadeView.Dispose ();
				FadeView = null;
			}

			if (TaskNameLabel != null) {
				TaskNameLabel.Dispose ();
				TaskNameLabel = null;
			}
		}
	}
}
