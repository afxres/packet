using System;
using System.Collections.Generic;
using System.Linq;

namespace Mikodev.Testing
{
    internal static class Extensions
    {
        public static void ThrowIfNotSequenceEqual<T>(IEnumerable<T> a, IEnumerable<T> b)
        {
            if (a.SequenceEqual(b))
                return;
            throw new ApplicationException();
        }

        public static void ThrowIfNotEqual<TK, TV>(IDictionary<TK, TV> a, IDictionary<TK, TV> b)
        {
            if (a.Count != b.Count)
                throw new ApplicationException();
            var cmp = EqualityComparer<TV>.Default;
            foreach (var i in a)
            {
                if (b.TryGetValue(i.Key, out var val) && cmp.Equals(val, i.Value))
                    continue;
                else throw new ApplicationException();
            }
        }

        public static void ThrowIfNotEqual<T>(ISet<T> a, ISet<T> b)
        {
            if (a.Count != b.Count)
                throw new ApplicationException();
            foreach (var i in a)
            {
                if (b.Contains(i))
                    continue;
                throw new ApplicationException();
            }
        }
    }
}
