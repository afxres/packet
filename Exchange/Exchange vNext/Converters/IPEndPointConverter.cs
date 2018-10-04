using System;
using System.Net;

namespace Mikodev.Binary.Converters
{
    internal sealed class IPEndPointConverter : Converter<IPEndPoint>
    {
        public IPEndPointConverter() : base(0) { }

        public override unsafe void ToBytes(Allocator allocator, IPEndPoint value)
        {
            if (value == null)
                return;
            var addressBytes = value.Address.GetAddressBytes();
            var addressLength = addressBytes.Length;
            fixed (byte* target = &allocator.Allocate(addressLength + sizeof(ushort)))
            {
                fixed (byte* source = &addressBytes[0])
                    Unsafe.Copy(target, source, addressLength);
                UnmanagedValueConverter<ushort>.UnsafeToBytes(target + addressLength, (ushort)value.Port);
            }
        }

        public override unsafe IPEndPoint ToValue(ReadOnlyMemory<byte> memory)
        {
            if (memory.IsEmpty)
                return null;
            var addressLength = memory.Length - sizeof(ushort);
            if (addressLength <= 0)
                ThrowHelper.ThrowOverflow();
            int port;
            var addressBytes = new byte[addressLength];
            fixed (byte* source = &memory.Span[0])
            {
                fixed (byte* target = &addressBytes[0])
                    Unsafe.Copy(target, source, addressLength);
                port = UnmanagedValueConverter<ushort>.UnsafeToValue(source + addressLength);
            }
            var address = new IPAddress(addressBytes);
            return new IPEndPoint(address, port);
        }
    }
}
