using System.Net;

namespace Mikodev.Binary.Converters
{
    internal sealed class IPAddressConverter : Converter<IPAddress>
    {
        public IPAddressConverter() : base(0) { }

        public override void ToBytes(Allocator allocator, IPAddress value)
        {
            if (value is null)
                return;
            var result = value.GetAddressBytes();
            allocator.Append(result);
        }

        public override IPAddress ToValue(Block block)
        {
            if (block.IsEmpty)
                return null;
            var result = block.ToArray();
            return new IPAddress(result);
        }
    }
}
