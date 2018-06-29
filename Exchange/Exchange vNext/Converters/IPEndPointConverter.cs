﻿using System.Net;
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
            Unsafe.CopyBlockUnaligned(ref block.Location, ref source[0], (uint)source.Length);
            UnmanagedValueConverter<ushort>.ToBytesUnchecked(ref block[source.Length], (ushort)value.Port);
        }

        public override IPEndPoint ToValue(Block block)
        {
            if (block.IsEmpty)
                return null;
            var addressBlock = block.Slice(0, block.Length - sizeof(ushort));
            var address = new IPAddress(addressBlock.ToArray());
            var port = UnmanagedValueConverter<ushort>.ToValueUnchecked(ref block[addressBlock.Length]);
            return new IPEndPoint(address, port);
        }
    }
}
