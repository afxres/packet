using System;

namespace Mikodev.Network
{
    public abstract class PacketConverter
    {
        public int Length { get; }

        protected PacketConverter(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            Length = length;
        }

        public abstract byte[] GetBytes(object value);

        public abstract object GetObject(byte[] buffer, int offset, int length);
    }

    public abstract class PacketConverter<T> : PacketConverter
    {
        protected PacketConverter(int length) : base(length) { }

        public abstract byte[] GetBytes(T value);

        public abstract T GetValue(byte[] buffer, int offset, int length);
    }
}
