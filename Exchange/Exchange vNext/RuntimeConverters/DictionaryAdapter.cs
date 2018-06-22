using System.Collections.Generic;

namespace Mikodev.Binary.RuntimeConverters
{
    internal readonly struct DictionaryAdapter<TK, TV>
    {
        private readonly Converter<TK> keyConverter;
        private readonly Converter<TV> valueConverter;

        internal DictionaryAdapter(Converter<TK> keyConverter, Converter<TV> valueConverter)
        {
            this.keyConverter = keyConverter;
            this.valueConverter = valueConverter;
        }

        internal void Bytes(Allocator allocator, IDictionary<TK, TV> value)
        {
            if (value == null || value.Count == 0)
                return;
            foreach (var i in value)
            {
                keyConverter.ToBytesAuto(allocator, i.Key);
                valueConverter.ToBytesAuto(allocator, i.Value);
            }
        }

        internal Dictionary<TK, TV> Value(Block block)
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
