namespace Mikodev.Network
{
    public abstract class PacketConverter
    {
        public abstract byte[] GetBytes(object value);

        public abstract object GetObject(byte[] buffer, int offset, int length);

        /// <summary>
        /// If length is variable, return zero.
        /// </summary>
        public abstract int Length { get; }
    }

    public abstract class PacketConverter<T> : PacketConverter
    {
        public abstract byte[] GetBytes(T value);

        public abstract T GetValue(byte[] buffer, int offset, int length);
    }
}
