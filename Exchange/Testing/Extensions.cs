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
    }
}
