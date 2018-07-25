using System;
using System.Net;

namespace Mikodev.Binary.Converters
{
    internal sealed class IPAddressConverter : Converter<IPAddress>
    {
        public IPAddressConverter() : base(0) { }

        public override void ToBytes(Allocator allocator, IPAddress value)
        {
            if (value == null)
                return;
            var result = value.GetAddressBytes();
            allocator.Append(result);
        }

        public override IPAddress ToValue(Memory<byte> memory)
        {
            if (memory.IsEmpty)
                return null;
            var result = memory.ToArray();
            return new IPAddress(result);
        }
    }
}
