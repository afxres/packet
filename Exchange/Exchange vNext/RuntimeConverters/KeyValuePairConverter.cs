using Mikodev.Binary.Common;
using System.Collections.Generic;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class KeyValuePairConverter<TK, TV> : Converter<KeyValuePair<TK, TV>>
    {
        private readonly Converter<TK> keyConverter;
        private readonly Converter<TV> valueConverter;

        public KeyValuePairConverter(Converter<TK> keyConverter, Converter<TV> valueConverter) : base(Extension.TupleLength(keyConverter, valueConverter))
        {
            this.keyConverter = keyConverter;
            this.valueConverter = valueConverter;
        }

        public override void ToBytes(Allocator allocator, KeyValuePair<TK, TV> value)
        {
            keyConverter.ToBytesExcept(allocator, value.Key);
            valueConverter.ToBytesExcept(allocator, value.Value);
        }

        public override KeyValuePair<TK, TV> ToValue(Block block)
        {
            var vernier = new Vernier(block);
            vernier.FlushExcept(keyConverter.Length);
            var key = keyConverter.ToValue(new Block(vernier));
            vernier.FlushExcept(valueConverter.Length);
            var value = valueConverter.ToValue(new Block(vernier));
            return new KeyValuePair<TK, TV>(key, value);
        }
    }
}
