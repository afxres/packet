using System.Collections.Generic;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class DictionaryConverter<TK, TV> : Converter<Dictionary<TK, TV>>
    {
        private readonly Converter<TK> keyConverter;
        private readonly Converter<TV> valueConverter;

        public DictionaryConverter(Converter<TK> keyConverter, Converter<TV> valueConverter) : base(0)
        {
            this.keyConverter = keyConverter;
            this.valueConverter = valueConverter;
        }

        public override void ToBytes(Allocator allocator, Dictionary<TK, TV> value) => IDictionaryConverter<TK, TV>.ToBytesNormal(allocator, value, keyConverter, valueConverter);

        public override Dictionary<TK, TV> ToValue(Block block) => IDictionaryConverter<TK, TV>.ToValueNormal(block, keyConverter, valueConverter);
    }
}
