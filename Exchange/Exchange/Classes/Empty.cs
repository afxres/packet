namespace Mikodev.Network
{
#if NETFULL
    internal static class Empty
    {
        internal static T[] Array<T>() => System.Array.Empty<T>();
    }
#else
    internal static class Empty<T>
    {
        internal static readonly T[] Array = new T[0];
    }

    internal static class Empty
    {
internal static T[] Array<T>() => Empty<T>.Array;
    }
#endif
}
