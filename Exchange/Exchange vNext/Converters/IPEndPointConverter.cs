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
            fixed (byte* dstptr = allocator.Allocate(addressLength + sizeof(ushort)))
            {
                fixed (byte* srcptr = &addressBytes[0])
                    Unsafe.Copy(dstptr, srcptr, addressLength);
                UnmanagedValueConverter<ushort>.UnsafeToBytes(dstptr + addressLength, (ushort)value.Port);
            }
        }

        public override unsafe IPEndPoint ToValue(ReadOnlySpan<byte> memory)
        {
            if (memory.IsEmpty)
                return null;
            var addressLength = memory.Length - sizeof(ushort);
            if (addressLength <= 0)
                ThrowHelper.ThrowOverflow();
            int port;
            var addressBytes = new byte[addressLength];
            fixed (byte* srcptr = memory)
            {
                fixed (byte* dstptr = &addressBytes[0])
                    Unsafe.Copy(dstptr, srcptr, addressLength);
                port = UnmanagedValueConverter<ushort>.UnsafeToValue(srcptr + addressLength);
            }
            var address = new IPAddress(addressBytes);
            return new IPEndPoint(address, port);
        }
    }
}
