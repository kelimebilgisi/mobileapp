using System;
using System.Collections.Generic;
using Toggl.Foundation.Interactors.Timezones;
using Toggl.Foundation.Serialization;

namespace Toggl.Foundation.Interactors
{
    public sealed partial class InteractorFactory
    {
        public IInteractor<IObservable<IEnumerable<string>>> GetSupportedTimezones() =>
            new GetSupportedTimezonesInteractor(new JsonSerializer());
    }
}
