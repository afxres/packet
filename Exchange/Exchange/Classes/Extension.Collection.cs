namespace Mikodev.Network
{
    partial class Extension
    {
#if NETFULL
        internal static T[] EmptyArray<T>() => System.Array.Empty<T>();
#else
        private static class Empty<T>
        {
            internal static readonly T[] Array = new T[0];
        }

        internal static T[] EmptyArray<T>() => Empty<T>.Array;
#endif
    }
}
