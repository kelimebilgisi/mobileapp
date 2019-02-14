using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Toggl.Foundation.DataSources.Interfaces;
using Toggl.Foundation.Models.Interfaces;
using Toggl.Multivac;
using Toggl.Multivac.Models;
using Toggl.PrimeRadiant.Models;

namespace Toggl.Foundation.Interactors.Generic
{
    public sealed class GetMultipleByIdInteractor<TThreadsafe, TDatabase> 
        : IInteractor<IObservable<IEnumerable<TThreadsafe>>>
        where TDatabase : IDatabaseModel, IIdentifiable
        where TThreadsafe : TDatabase, IThreadSafeModel
    {
        private readonly IDataSource<TThreadsafe, TDatabase> dataSource;
        private readonly long[] ids;

        public GetMultipleByIdInteractor(IDataSource<TThreadsafe, TDatabase> dataSource, long[] ids)
        {
            Ensure.Argument.IsNotNull(dataSource, nameof(dataSource));

            this.dataSource = dataSource;
            this.ids = ids;
        }

        public IObservable<IEnumerable<TThreadsafe>> Execute()
            => dataSource.GetAll(entry => ids.Contains(entry.Id));
    }
}
