using Microsoft.Reactive.Testing;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.Tests.TestExtensions
{
    public static class RxActionExtensions
    {
        public static async Task Execute(this UIAction action, TestScheduler testScheduler)
        {
            var toAwait = action.Execute();
            testScheduler.Start();
            await toAwait;
        }

        public static async Task Execute<T>(this InputAction<T> action, T input, TestScheduler testScheduler)
        {
            var toAwait = action.Execute(input);
            testScheduler.Start();
            await toAwait;
        }
    }
}
