using Foundation;
using Toggl.Foundation.MvvmCross.Collections;

namespace Toggl.Daneel.Extensions
{
    public static class SectionedIndexExtensions
    {
        public static NSIndexPath ToIndexPath(this SectionedIndex index)
            => NSIndexPath.FromRowSection(index.Row, index.Section);

        public static NSIndexPath[] ToIndexPaths(this SectionedIndex index)
            => new[] { index.ToIndexPath() };
    }
}
