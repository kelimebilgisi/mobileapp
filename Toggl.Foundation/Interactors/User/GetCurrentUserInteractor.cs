using System;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;

namespace Toggl.Foundation.Interactors
{
    internal sealed class GetCurrentUserInteractor : IInteractor<IObservable<IThreadSafeUser>>
    {
        private readonly ISingletonDataSource<IThreadSafeUser> dataSource;

        public GetCurrentUserInteractor(ISingletonDataSource<IThreadSafeUser> dataSource)
        {
            this.dataSource = dataSource;
        }

        public IObservable<IThreadSafeUser> Execute()
            => dataSource.Current;
    }
}
