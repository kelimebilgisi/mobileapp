using System;
using System.Reactive.Linq;
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

            var hasProject = ViewModel.ProjectClientTask.Select(projectClientTask => projectClientTask.HasProject);

            new CategorizeTimeUsingProjectsOnboardingStep(storage, hasProject)
                .ManageDismissableTooltip(
                    projectTooltip,
                    projectButton,
                    (window, view) => PopupOffsets.FromDp(16, 8, this),
                    storage)
                .DisposedBy(DisposeBag);
        }
    }
}
