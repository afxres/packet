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
            var source = value.Address.GetAddressBytes();
            var length = source.Length;
            ref var target = ref allocator.Allocate(length + sizeof(ushort));
            fixed (byte* src = &source[0])
            fixed (byte* dst = &target)
            {
                Unsafe.Copy(dst, src, length);
                UnmanagedValueConverter<ushort>.UnsafeToBytes(dst + length, (ushort)value.Port);
            }
        }

        public override IPEndPoint ToValue(ReadOnlyMemory<byte> memory)
        {
            if (memory.IsEmpty)
                return null;
            var span = memory.Span;
            var addressLength = span.Length - sizeof(ushort);
            if (addressLength <= 0)
                ThrowHelper.ThrowOverflow();
            var addressBytes = new byte[addressLength];
            Unsafe.Copy(ref addressBytes[0], in span[0], addressLength);
            var address = new IPAddress(addressBytes);
            var port = UnmanagedValueConverter<ushort>.UnsafeToValue(in span[addressLength]);
            return new IPEndPoint(address, port);
        }
    }
}
