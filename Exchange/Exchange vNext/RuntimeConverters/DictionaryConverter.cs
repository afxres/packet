using System.Collections.Generic;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class DictionaryConverter<TK, TV> : Converter<Dictionary<TK, TV>>
    {
        private readonly DictionaryAdapter<TK, TV> adapter;

        public DictionaryConverter(Converter<TK> keyConverter, Converter<TV> valueConverter) : base(0) => adapter = new DictionaryAdapter<TK, TV>(keyConverter, valueConverter);

        public override void ToBytes(Allocator allocator, Dictionary<TK, TV> value) => adapter.ToBytes(allocator, value);

        public override Dictionary<TK, TV> ToValue(Block block) => adapter.ToValue(block);
    }
}
