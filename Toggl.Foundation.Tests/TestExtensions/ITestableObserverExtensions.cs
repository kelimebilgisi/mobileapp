using System.Collections.Generic;
using Microsoft.Reactive.Testing;
using System.Linq;
using System.Reactive;

namespace Toggl.Foundation.Tests.TestExtensions
{
    public static class ITestableObserverExtensions
    {
        public static T SingleEmittedValue<T>(this ITestableObserver<T> observer)
            => observer.Messages.Single().Value.Value;

        public static T LastEmittedValue<T>(this ITestableObserver<T> observer)
            => observer.Messages.Last().Value.Value;

        public static IEnumerable<T> Values<T>(this ITestableObserver<T> observer)
            => observer.Messages
                .Select(recorded => recorded.Value)
                .Where(notification => notification.Kind == NotificationKind.OnNext)
                .Select(notification => notification.Value);
    }
}
