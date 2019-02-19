using System;
using System.Reactive;
using System.Reactive.Disposables;
using Microsoft.Reactive.Testing;

namespace Toggl.Foundation.Tests.TestExtensions
{
    public static class TestSchedulerExtensions
    {
        public static void Schedule(this TestScheduler self, TimeSpan ticks, Action action)
            => self.Schedule(Unit.Default, ticks, (_, __) =>
            {
                action();
                return Disposable.Empty;
            });
    }
}
