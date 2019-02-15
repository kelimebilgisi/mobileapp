using System;
using Android.Widget;
using Toggl.Foundation.MvvmCross.Onboarding.EditView;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Helper;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Activities
{
    public sealed partial class EditTimeEntryActivity 
    {
        private PopupWindow projectTooltip;

        private void resetOnboardingOnResume()
        {
            projectTooltip = projectTooltip
                ?? PopupWindowFactory.PopupWindowWithText(
                    this,
                    Resource.Layout.TooltipWithLeftTopArrow,
                    Resource.Id.TooltipText,
                    Resource.String.CategorizeWithProjects);

            prepareOnboarding();
        }

        private void clearOnboardingOnStop()
        {
            projectTooltip.Dismiss();
            projectTooltip = null;
        }

        private void prepareOnboarding()
        {
            var storage = ViewModel.OnboardingStorage;

            new CategorizeTimeUsingProjectsOnboardingStep(storage, ViewModel.HasProject)
                .ManageDismissableTooltip(
                    projectTooltip,
                    projectButton,
                    (window, view) => PopupOffsets.FromDp(16, 8, this),
                    storage)
                .DisposedBy(DisposeBag);
        }
    }
}
