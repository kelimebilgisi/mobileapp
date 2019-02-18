using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Toggl.Foundation.Sync;
using Toggl.Multivac.Extensions;

namespace Toggl.Foundation.Tests.Sync
{
    internal sealed class TestConfigurator : ITransitionConfigurator
    {
        public List<object> AllDistinctStatesInOrder { get; } = new List<object>();

        public Dictionary<IStateResult, (object State, Type ParameterType)> Transitions { get; }
            = new Dictionary<IStateResult, (object, Type)>();

        public void ConfigureTransition(IStateResult result, ISyncState state)
        {
            addToListIfNew(state);
            Transitions.Add(result, (state, null));
        }

        public void ConfigureTransition<T>(StateResult<T> result, ISyncState<T> state)
        {
            addToListIfNew(state);
            Transitions.Add(result, (state, typeof(T)));
        }

        private void addToListIfNew(object state)
        {
            if (AllDistinctStatesInOrder.Contains(state))
                return;

            AllDistinctStatesInOrder.Add(state);
        }

        public IEnumerable<NamedStateResult> GetAllLooseEndStateResults()
        {
            return getAllStateResults()
                .Where(r => !Transitions.ContainsKey(r.Result));
        }

        private IEnumerable<NamedStateResult> getAllStateResults()
        {
            return getAllStatesWithTheirResults()
                .SelectMany(state => state.NamedResults);
        }

        private IEnumerable<(object State, List<NamedStateResult> NamedResults)> getAllStatesWithTheirResults()
        {
            return AllDistinctStatesInOrder
                .Select(state => (state, stateResultsOf(state)));
        }

        private static List<NamedStateResult> stateResultsOf(object state)
        {
            return state.GetType()
                .GetProperties()
                .Where(isStateResultProperty)
                .Select(p => new NamedStateResult(state, (IStateResult)p.GetValue(state), p.Name))
                .ToList();
        }

        private static bool isStateResultProperty(PropertyInfo property)
        {
            return property.PropertyType.ImplementsOrDerivesFrom<IStateResult>();
        }
    }
}
