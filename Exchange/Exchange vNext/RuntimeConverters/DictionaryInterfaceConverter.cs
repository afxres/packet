using System.Collections.Generic;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class DictionaryInterfaceConverter<TK, TV> : Converter<IDictionary<TK, TV>>
    {
        private readonly DictionaryAdapter<TK, TV> adapter;

        public DictionaryInterfaceConverter(Converter<TK> keyConverter, Converter<TV> valueConverter) : base(0) => adapter = new DictionaryAdapter<TK, TV>(keyConverter, valueConverter);

        public override void ToBytes(Allocator allocator, IDictionary<TK, TV> value) => adapter.ToBytes(allocator, value);

        public override IDictionary<TK, TV> ToValue(Block block) => adapter.ToValue(block);
    }
}
