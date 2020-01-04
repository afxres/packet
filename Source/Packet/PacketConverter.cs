using System;

namespace Mikodev.Network
{
    public abstract class PacketConverter
    {
        public int Length { get; }

        internal PacketConverter(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            this.Length = length;
        }

        public abstract byte[] GetBytes(object value);

        public abstract object GetObject(byte[] buffer, int offset, int length);
    }

    public abstract class PacketConverter<T> : PacketConverter
    {
        protected PacketConverter(int length) : base(length) { }

        public abstract byte[] GetBytes(T value);

        public abstract T GetValue(byte[] buffer, int offset, int length);

        public override byte[] GetBytes(object value) => this.GetBytes((T)value);

        public override object GetObject(byte[] buffer, int offset, int length) => this.GetValue(buffer, offset, length);
    }
}
