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
            var block = allocator.Allocate(source.Length + sizeof(ushort));
            Unsafe.CopyBlockUnaligned(ref block[0], ref source[0], (uint)source.Length);
            UnmanagedValueConverter<ushort>.ToBytesUnchecked(ref block[source.Length], (ushort)value.Port);
        }

        public override IPEndPoint ToValue(Block block)
        {
            if (block.IsEmpty)
                return null;
            var addressLength = block.Length - sizeof(ushort);
            if (addressLength <= 0)
                ThrowHelper.ThrowOverflow();
            var addressBytes = new byte[addressLength];
            Unsafe.CopyBlockUnaligned(ref addressBytes[0], ref block[0], (uint)addressLength);
            var address = new IPAddress(addressBytes);
            var port = UnmanagedValueConverter<ushort>.ToValueUnchecked(ref block[addressLength]);
            return new IPEndPoint(address, port);
        }
    }
}
