using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.Tests.TestExtensions
{
    public static class RxActionExtensions
    {
        public static IObservable<TOutput> ExecuteWithObservable<TInput, TOutput>(this RxAction<TInput, TOutput> action,
            TInput input)
        {
            var subject = new ReplaySubject<TOutput>();

            var error = action.Errors
                .SelectMany(e => Observable.Throw<TOutput>(e));

            action.Elements
                .Amb(error)
                .Take(1)
                .Select(CommonFunctions.Identity)
                .Subscribe(subject);

            action.Inputs.OnNext(input);
            return subject.AsObservable();
        }

        public static IObservable<TOutput> ExecuteSequentally<TInput, TOutput>(this RxAction<TInput, TOutput> action,
            IEnumerable<TInput> inputs)
        {
            return Observable.Concat(
                inputs
                    .Select(input => action.ExecuteWithObservable(input))
                    .ToArray()
            );
        }

        public static IObservable<TOutput> ExecuteSequentally<TInput, TOutput>(this RxAction<TInput, TOutput> action,
            params TInput[] inputs)
            => action.ExecuteSequentally(inputs);

        public static IObservable<Unit> ExecuteSequentally(this UIAction action,
            int times)
        {
            return Observable.Concat(
                Enumerable.Range(0, times)
                    .Select(_ => action.ExecuteWithObservable(default(Unit)))
                    .ToArray()
            );
        }
    }
}
