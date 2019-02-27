using System;

namespace Toggl.Multivac
{
    public enum WorkspaceFeatureId
    {
        Free = 0,
        Pro = 13,
        [Obsolete("Use specific granular features instead.", true)]
        Business = 15,
        ScheduledReports = 50,
        TimeAudits = 51,
        LockingTimeEntries = 52,
        EditTeamMemberTimeEntries = 53,
        EditTeamMemberProfile = 54,
        TrackingReminders = 55,
        TimeEntryConstraints = 56,
        PrioritySupport = 57,
        LabourCost = 58,
        ReportEmployeeProfitability = 59,
        ReportProjectProfitability = 60,
        ReportComparative = 61,
        ReportDataTrends = 62,
        ReportExportXlsx = 63,
        Tasks = 64,
    }
}
