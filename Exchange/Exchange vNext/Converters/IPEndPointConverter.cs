using System;
using System.Net;

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
            Unsafe.Copy(ref span[0], in source[0], source.Length);
            UnmanagedValueConverter<ushort>.UnsafeToBytes(ref span[source.Length], (ushort)value.Port);
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
