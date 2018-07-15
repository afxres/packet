namespace Mikodev.Network
{
#if !NETFULL
    internal static class Empty<T>
    {
        internal static readonly T[] Array = new T[0];
    }
#endif

    internal static class Empty
    {
#if !NETFULL
        internal static T[] Array<T>() => Empty<T>.Array;
#else
        internal static T[] Array<T>() => System.Array.Empty<T>();
#endif
    }
}
