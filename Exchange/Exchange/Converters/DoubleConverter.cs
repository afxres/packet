using System;

namespace Mikodev.Network.Converters
{
    [PacketConverter(typeof(Double))]
    internal sealed class DoubleConverter : PacketConverter<Double>
    {
        public override int Length => sizeof(Double);

        public override byte[] GetBytes(Double value) => BitConverter.GetBytes(value);

        public override Double GetValue(byte[] buffer, int offset, int length) => BitConverter.ToDouble(buffer, offset);

        public override byte[] GetBuffer(object value) => BitConverter.GetBytes((Double)value);

        public override object GetObject(byte[] buffer, int offset, int length) => BitConverter.ToDouble(buffer, offset);
    }
}
