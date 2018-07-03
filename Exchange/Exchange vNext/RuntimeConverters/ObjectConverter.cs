namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class ObjectConverter : Converter<object>
    {
        private readonly Cache packetCache;

        public ObjectConverter(Cache packetCache) : base(0) => this.packetCache = packetCache;

        public override void ToBytes(Allocator allocator, object value)
        {
            if (value == null)
                return;
            var converter = packetCache.GetConverter(value.GetType());
            converter.ToBytesAny(allocator, value);
        }

        public override object ToValue(Block block) => new Token(packetCache, block);
    }
}
