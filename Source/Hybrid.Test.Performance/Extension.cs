using System.Collections.Generic;

namespace Sample
{
    internal static class Extension
    {
        internal static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable) => new HashSet<T>(enumerable);
    }
}
