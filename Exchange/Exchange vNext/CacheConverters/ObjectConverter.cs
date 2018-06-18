using Mikodev.Binary.Common;
using System;

namespace Mikodev.Binary.CacheConverters
{
    internal sealed class ObjectConverter : Converter<object>
    {
        private readonly PacketCache packetCache;

        public ObjectConverter(PacketCache packetCache) : base(0) => this.packetCache = packetCache;

        public override void ToBytes(Allocator allocator, object value)
        {
            if (value == null)
                return;
            var converter = packetCache.GetOrCreateConverter(value.GetType());
            converter.ToBytesNonGeneric(allocator, value);
        }

        public override object ToValue(Block block) => throw new InvalidOperationException();
    }
}
