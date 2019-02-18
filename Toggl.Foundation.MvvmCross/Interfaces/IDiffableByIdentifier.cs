using System;

namespace Toggl.Foundation.MvvmCross.Interfaces
{
    public interface IDiffableByIdentifier<T> : IEquatable<T>
    {
        long Identifier { get; }
    }
}
