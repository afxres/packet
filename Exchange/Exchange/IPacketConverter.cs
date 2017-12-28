namespace Mikodev.Network
{
    public interface IPacketConverter
    {
        byte[] GetBytes(object value);

        object GetValue(byte[] buffer, int offset, int length);

        /// <summary>
        /// If length is variable, return zero.
        /// </summary>
        int Length { get; }
    }

    public interface IPacketConverter<T> : IPacketConverter
    {
        byte[] GetBytes(T value);

        new T GetValue(byte[] buffer, int offset, int length);
    }
}
