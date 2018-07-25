using System;
using System.Net;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary.Converters
{
    internal sealed class IPEndPointConverter : Converter<IPEndPoint>
    {
        public IPEndPointConverter() : base(0) { }

        public override void ToBytes(Allocator allocator, IPEndPoint value)
        {
            if (value == null)
                return;
            var source = value.Address.GetAddressBytes();
            var memory = allocator.Allocate(source.Length + sizeof(ushort));
            var span = memory.Span;
            Unsafe.CopyBlockUnaligned(ref span[0], ref source[0], (uint)source.Length);
            UnmanagedValueConverter<ushort>.UnsafeToBytes(ref span[source.Length], (ushort)value.Port);
        }

        public override IPEndPoint ToValue(Memory<byte> memory)
        {
            if (memory.IsEmpty)
                return null;
            var span = memory.Span;
            var addressLength = span.Length - sizeof(ushort);
            if (addressLength <= 0)
                ThrowHelper.ThrowOverflow();
            var addressBytes = new byte[addressLength];
            Unsafe.CopyBlockUnaligned(ref addressBytes[0], ref span[0], (uint)addressLength);
            var address = new IPAddress(addressBytes);
            var port = UnmanagedValueConverter<ushort>.UnsafeToValue(ref span[addressLength]);
            return new IPEndPoint(address, port);
        }
    }
}
