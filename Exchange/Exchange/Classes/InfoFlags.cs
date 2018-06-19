namespace Mikodev.Network
{
    internal enum InfoFlags : int
    {
        None = 0,
        Invalid, // throw invalid type
        Reader,
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
