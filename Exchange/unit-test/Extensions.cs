using System;

namespace Mikodev.UnitTest
{
    internal static class Extensions
    {
        public static void ThrowIfNotAllEquals<T>(T[] a, T[] b)
        {
            if (a.Length != b.Length)
                throw new ApplicationException();
            for (int i = 0; i < a.Length && i < b.Length; i++)
                if (a[i].Equals(b[i]) == false)
                    throw new ApplicationException();
            return;
        }
    }
}
