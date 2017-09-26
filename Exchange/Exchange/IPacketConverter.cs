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
        /// byte length of target type, return null if length not fixed
        /// </summary>
        int? Length { get; }
    }

    internal interface IPacketConverter<T>
    {
        byte[] GetBytes(T value);

        T GetValue(byte[] buffer, int offset, int length);

        int? Length { get; }
    }
}
