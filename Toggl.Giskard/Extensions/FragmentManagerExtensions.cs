using System.Linq;
using Android.Support.V4.App;
using Toggl.Multivac.Extensions;

namespace Toggl.Giskard.Extensions
{
    public static class FragmentManagerExtensions
    {
        public static void RemoveAllFragments(this FragmentManager fragmentManager)
        {
            fragmentManager.Fragments
                .Aggregate(fragmentManager.BeginTransaction(), (transaction, fragment) => transaction.Remove(fragment))
                .Commit();
        }
    }
}
