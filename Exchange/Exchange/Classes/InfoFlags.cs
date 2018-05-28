namespace Mikodev.Network
{
    internal enum InfoFlags : int
    {
        None = 0,
        Enum = 4,

        Reader = 16,
        RawReader,
        Collection,
        Enumerable,
        Dictionary,
        Expando, // Dictionary<string, object>
        Bytes,
        SBytes,
        Writer,
        RawWriter,
    }
}
