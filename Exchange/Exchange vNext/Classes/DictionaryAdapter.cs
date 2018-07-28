using System;
using System.Collections.Generic;

namespace Mikodev.Binary
{
    internal abstract class DictionaryAdapter
    {
        public abstract Delegate BytesDelegate { get; }
        public abstract Delegate ValueDelegate { get; }
        public abstract Delegate TupleDelegate { get; }
    }

    internal sealed class DictionaryAdapter<TK, TV> : DictionaryAdapter
    {
        private readonly Converter<TK> keyConverter;
        private readonly Converter<TV> valueConverter;

        public sealed override Delegate BytesDelegate { get; }
        public sealed override Delegate ValueDelegate { get; }
        public sealed override Delegate TupleDelegate { get; }

        public DictionaryAdapter(Converter<TK> keyConverter, Converter<TV> valueConverter)
        {
            this.keyConverter = keyConverter;
            this.valueConverter = valueConverter;
            BytesDelegate = new Action<Allocator, IEnumerable<KeyValuePair<TK, TV>>>(Bytes);
            ValueDelegate = new Func<ReadOnlyMemory<byte>, Dictionary<TK, TV>>(Value);
            TupleDelegate = new Func<ReadOnlyMemory<byte>, List<Tuple<TK, TV>>>(Tuple);
        }

        public void Bytes(Allocator allocator, IEnumerable<KeyValuePair<TK, TV>> value)
        {
            if (value == null)
                return;
            int offset;
            foreach (var i in value)
            {
                if (keyConverter.Length == 0)
                {
                    offset = allocator.AnchorExtend();
                    keyConverter.ToBytes(allocator, i.Key);
                    allocator.FinishExtend(offset);
                }
                else
                {
                    keyConverter.ToBytes(allocator, i.Key);
                }
                if (valueConverter.Length == 0)
                {
                    offset = allocator.AnchorExtend();
                    valueConverter.ToBytes(allocator, i.Value);
                    allocator.FinishExtend(offset);
                }
                else
                {
                    valueConverter.ToBytes(allocator, i.Value);
                }
            }
        }

        public Dictionary<TK, TV> Value(ReadOnlyMemory<byte> memory)
        {
            if (memory.IsEmpty)
                return new Dictionary<TK, TV>(0);
            var dictionary = new Dictionary<TK, TV>(8);
            ref readonly var location = ref memory.Span[0];
            var vernier = new Vernier(memory.Length);
            while (vernier.Any())
            {
                vernier.FlushExcept(in location, keyConverter.Length);
                var key = keyConverter.ToValue(memory.Slice(vernier.offset, vernier.length));
                vernier.FlushExcept(in location, valueConverter.Length);
                var value = valueConverter.ToValue(memory.Slice(vernier.offset, vernier.length));
                dictionary.Add(key, value);
            }
            return dictionary;
        }

        public List<Tuple<TK, TV>> Tuple(ReadOnlyMemory<byte> memory)
        {
            if (memory.IsEmpty)
                return new List<Tuple<TK, TV>>(0);
            var list = new List<Tuple<TK, TV>>(8);
            ref readonly var location = ref memory.Span[0];
            var vernier = new Vernier(memory.Length);
            while (vernier.Any())
            {
                vernier.FlushExcept(in location, keyConverter.Length);
                var key = keyConverter.ToValue(memory.Slice(vernier.offset, vernier.length));
                vernier.FlushExcept(in location, valueConverter.Length);
                var value = valueConverter.ToValue(memory.Slice(vernier.offset, vernier.length));
                list.Add(new Tuple<TK, TV>(key, value));
            }
            return list;
        }
    }
}
