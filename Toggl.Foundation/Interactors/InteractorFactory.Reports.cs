using System;
using Toggl.Foundation.Interactors.Reports;
using Toggl.Multivac.Models.Reports;

namespace Toggl.Foundation.Interactors
{
    public partial class InteractorFactory
    {
        public IInteractor<IObservable<ITimeEntriesTotals>> GetReportsTotals(
            long userId, long workspaceId, DateTimeOffset startDate, DateTimeOffset endDate)
            => new GetTotalsInteractor(api.TimeEntriesReports, userId, workspaceId, startDate, endDate);
    }
}
