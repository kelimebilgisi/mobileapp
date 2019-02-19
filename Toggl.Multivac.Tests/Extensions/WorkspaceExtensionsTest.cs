using FluentAssertions;
using NSubstitute;
using Toggl.Multivac.Extensions;
using Toggl.Multivac.Models;
using Xunit;

namespace Toggl.Multivac.Tests
{
    public sealed class WorkspaceExtensionsTest
    {
        public sealed class IsEligibleForProjectCreationTest
        {
            [Theory, LogIfTooSlow]
            [InlineData(false, false, true)]
            [InlineData(true, false, true)]
            [InlineData(false, true, false)]
            [InlineData(true, true, true)]
            public void ReturnsAppropriateValue(
                bool isAdmin,
                bool onlyAdminCanCreateProject,
                bool isEligibleForProjectCreation)
            {
                var workspace = Substitute.For<IWorkspace>();

                workspace.Admin.Returns(isAdmin);
                workspace.OnlyAdminsMayCreateProjects.Returns(onlyAdminCanCreateProject);

                workspace.IsEligibleForProjectCreation().Should().Be(isEligibleForProjectCreation);
            }
        }
    }
}
