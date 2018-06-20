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

        public override void ToBytes(Allocator allocator, Dictionary<TK, TV> value)
        {
            if (value == null || value.Count == 0)
                return;
            int offset;
            var stream = allocator.stream;
            foreach (var i in value)
            {
                if (keyConverter.Length == 0)
                {
                    offset = stream.BeginModify();
                    keyConverter.ToBytes(allocator, i.Key);
                    stream.EndModify(offset);
                }
                else
                {
                    keyConverter.ToBytes(allocator, i.Key);
                }
                if (valueConverter.Length == 0)
                {
                    offset = stream.BeginModify();
                    valueConverter.ToBytes(allocator, i.Value);
                    stream.EndModify(offset);
                }
                else
                {
                    valueConverter.ToBytes(allocator, i.Value);
                }
            }
        }

        public override Dictionary<TK, TV> ToValue(Block block)
        {
            if (block.IsEmpty)
                return new Dictionary<TK, TV>();
            var dictionary = new Dictionary<TK, TV>(8);
            var vernier = (Vernier)block;
            while (vernier.Any)
            {
                vernier.FlushExcept(keyConverter.Length);
                var key = keyConverter.ToValue((Block)vernier);
                vernier.FlushExcept(valueConverter.Length);
                var value = valueConverter.ToValue((Block)vernier);
                dictionary.Add(key, value);
            }
            return dictionary;
        }
    }
}
