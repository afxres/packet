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
        /// Path error
        /// </summary>
        PathError,

        /// <summary>
        /// Type invalid
        /// </summary>
        TypeInvalid,

        /// <summary>
        /// Data length overflow
        /// </summary>
        Overflow,

        /// <summary>
        /// Recursion limit has been reached
        /// </summary>
        RecursiveError,

        /// <summary>
        /// This error should not be thrown
        /// </summary>
        AssertFailed,

        /// <summary>
        /// Convert operation error
        /// </summary>
        ConvertError,
    }
}
