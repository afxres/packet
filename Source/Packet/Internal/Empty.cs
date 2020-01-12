namespace Mikodev.Network.Internal
{
    internal static class Empty<T>
    {
        internal static readonly T[] Array = new T[0];
    }

    internal static class Empty
    {
        internal static T[] Array<T>() => Empty<T>.Array;
    }
}
