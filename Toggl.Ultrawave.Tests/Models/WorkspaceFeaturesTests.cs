using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using FluentAssertions;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using Toggl.Ultrawave.Serialization;
using Toggl.Ultrawave.Models;

namespace Toggl.Ultrawave.Tests.Models
{
    public sealed class WorkspaceFeaturesTests
    {
        public sealed class TheWorkspaceFeaturesModel
        {
            [Fact, LogIfTooSlow]
            public void DeserializationOfOneWorkspace()
            {
                string arrayJson = $"[{ValidJsonFreeWorkspace}]";

                var result = deserialize(arrayJson).First();

                result.WorkspaceId.Should().Be(1);
                result.Features.Should().HaveCount(featuresCount);
                result.IsEnabled(WorkspaceFeatureId.Free).Should().BeTrue();
                result.IsEnabled(WorkspaceFeatureId.Pro).Should().BeFalse();
            }

            [Fact, LogIfTooSlow]
            public void DeserializationOfTwoWorkspaces()
            {
                string arrayJson = $"[{ValidJsonFreeWorkspace},{ValidJsonProWorkspace}]";

                var result = deserialize(arrayJson);

                result.Count.Should().Be(2);
                result[0].WorkspaceId.Should().Be(1);
                result[0].Features.Should().HaveCount(featuresCount);
                result[1].WorkspaceId.Should().Be(2);
                result[1].Features.Should().HaveCount(featuresCount);
            }

            private readonly JsonSerializer serializer = new JsonSerializer();

            private const string ValidJsonFreeWorkspace = "{\"workspace_id\":1,\"features\":[{\"feature_id\":0,\"name\":\"free\",\"enabled\":true},{\"feature_id\":13,\"name\":\"pro\",\"enabled\":false},{\"feature_id\":15,\"name\":\"business\",\"enabled\":false},{\"feature_id\":50,\"name\":\"scheduled_reports\",\"enabled\":false},{\"feature_id\":51,\"name\":\"time_audits\",\"enabled\":false},{\"feature_id\":52,\"name\":\"locking_time_entries\",\"enabled\":false},{\"feature_id\":53,\"name\":\"edit_team_member_time_entries\",\"enabled\":false},{\"feature_id\":54,\"name\":\"edit_team_member_profile\",\"enabled\":false},{\"feature_id\":55,\"name\":\"tracking_reminders\",\"enabled\":false},{\"feature_id\":56,\"name\":\"time_entry_constraints\",\"enabled\":false},{\"feature_id\":57,\"name\":\"priority_support\",\"enabled\":false},{\"feature_id\":58,\"name\":\"labour_cost\",\"enabled\":false},{\"feature_id\":59,\"name\":\"report_employee_profitability\",\"enabled\":false},{\"feature_id\":60,\"name\":\"report_project_profitability\",\"enabled\":false},{\"feature_id\":61,\"name\":\"report_comparative\",\"enabled\":false},{\"feature_id\":62,\"name\":\"report_data_trends\",\"enabled\":false},{\"feature_id\":63,\"name\":\"report_export_xlsx\",\"enabled\":false},{\"feature_id\":64,\"name\":\"tasks\",\"enabled\":false}]}";

            private const string ValidJsonProWorkspace = "{\"workspace_id\":2,\"features\":[{\"feature_id\":0,\"name\":\"free\",\"enabled\":true},{\"feature_id\":13,\"name\":\"pro\",\"enabled\":true},{\"feature_id\":15,\"name\":\"business\",\"enabled\":false},{\"feature_id\":50,\"name\":\"scheduled_reports\",\"enabled\":false},{\"feature_id\":51,\"name\":\"time_audits\",\"enabled\":false},{\"feature_id\":52,\"name\":\"locking_time_entries\",\"enabled\":false},{\"feature_id\":53,\"name\":\"edit_team_member_time_entries\",\"enabled\":false},{\"feature_id\":54,\"name\":\"edit_team_member_profile\",\"enabled\":false},{\"feature_id\":55,\"name\":\"tracking_reminders\",\"enabled\":false},{\"feature_id\":56,\"name\":\"time_entry_constraints\",\"enabled\":false},{\"feature_id\":57,\"name\":\"priority_support\",\"enabled\":false},{\"feature_id\":58,\"name\":\"labour_cost\",\"enabled\":false},{\"feature_id\":59,\"name\":\"report_employee_profitability\",\"enabled\":false},{\"feature_id\":60,\"name\":\"report_project_profitability\",\"enabled\":false},{\"feature_id\":61,\"name\":\"report_comparative\",\"enabled\":false},{\"feature_id\":62,\"name\":\"report_data_trends\",\"enabled\":false},{\"feature_id\":63,\"name\":\"report_export_xlsx\",\"enabled\":false},{\"feature_id\":64,\"name\":\"tasks\",\"enabled\":false}]}";

            private int featuresCount => Enum.GetValues(typeof(WorkspaceFeatureId)).Length;

            private List<WorkspaceFeatureCollection> deserialize(string json) =>
                serializer.Deserialize<List<WorkspaceFeatureCollection>>(json);
        }
    }
}
