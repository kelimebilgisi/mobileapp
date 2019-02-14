using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Toggl.Foundation.Analytics;
using Toggl.Foundation.DataSources;
using Toggl.Foundation.Extensions;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.Sync.States.CleanUp
{
    public sealed class CheckForInaccessibleWorkspacesState : ISyncState
    {
        private readonly ITogglDataSource dataSource;

        public StateResult NoInaccessibleWorkspaceFound { get; } = new StateResult();
        public StateResult FoundInaccessibleWorkspaces { get; } = new StateResult();

        public CheckForInaccessibleWorkspacesState(ITogglDataSource dataSource)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));

            this.dataSource = dataSource;
        }

        public IObservable<ITransition> Start()
            => dataSource
                .Workspaces
                .GetAll(ws => ws.IsInaccessible, includeInaccessibleEntities: true)
                .Select(data => data.Any())
                .Select(toTransition);

        private ITransition toTransition(bool hasInaccessibleWorkspaces)
            => hasInaccessibleWorkspaces
                ? FoundInaccessibleWorkspaces.Transition()
                : NoInaccessibleWorkspaceFound.Transition();
    }
}
