using Mikodev.Binary.Common;
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
            foreach (var i in value)
            {
                keyConverter.ToBytesExcept(allocator, i.Key);
                valueConverter.ToBytesExcept(allocator, i.Value);
            }
        }

        public override Dictionary<TK, TV> ToValue(Block block)
        {
            if (block.IsEmpty)
                return new Dictionary<TK, TV>();
            var dictionary = new Dictionary<TK, TV>(8);
            var vernier = new Vernier(block);
            while (vernier.Any)
            {
                vernier.FlushExcept(keyConverter.Length);
                var key = keyConverter.ToValue(new Block(vernier));
                vernier.FlushExcept(valueConverter.Length);
                var value = valueConverter.ToValue(new Block(vernier));
                dictionary.Add(key, value);
            }
            return dictionary;
        }
    }
}
