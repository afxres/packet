namespace Mikodev.Network
{
    /// <summary>
    /// Basic interface for converter
    /// </summary>
    public interface IPacketConverter
    {
        /// <summary>
        /// object -> byte array
        /// </summary>
        byte[] GetBytes(object value);

        /// <summary>
        /// byte array -> object
        /// </summary>
        object GetValue(byte[] buffer, int offset, int length);

        /// <summary>
        /// byte length, return null if length not fixed
        /// </summary>
        int Length { get; }
    }

    /// <summary>
    /// Generic interface for converter
    /// </summary>
    public interface IPacketConverter<T> : IPacketConverter
    {
        /// <summary>
        /// T -> byte array
        /// </summary>
        byte[] GetBytes(T value);

        /// <summary>
        /// byte array -> T
        /// </summary>
        new T GetValue(byte[] buffer, int offset, int length);
    }
}
