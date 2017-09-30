namespace Mikodev.Network
{
    /// <summary>
    /// Error code
    /// </summary>
    public enum PacketError
    {
        /// <summary>
        /// Default
        /// </summary>
        None,

        /// <summary>
        /// Convert operation error
        /// </summary>
        ConvertError,

        /// <summary>
        /// Data length overflow
        /// </summary>
        Overflow,

        /// <summary>
        /// Path error
        /// </summary>
        PathError,

        /// <summary>
        /// Recursion limit has been reached
        /// </summary>
        RecursiveError,

        /// <summary>
        /// Type invalid
        /// </summary>
        TypeInvalid,
    }
}
