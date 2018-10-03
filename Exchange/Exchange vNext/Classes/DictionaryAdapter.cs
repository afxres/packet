using System;
using System.Collections.Generic;

namespace Mikodev.Binary
{
    internal abstract class DictionaryAdapter
    {
        public abstract Delegate GetToBytesDelegate();

        public abstract Delegate GetToValueDelegate();

        public abstract Delegate GetToTupleDelegate();
    }

    internal sealed class DictionaryAdapter<TK, TV> : DictionaryAdapter
    {
        private readonly Converter<TK> keyConverter;

        private readonly Converter<TV> valueConverter;

        public DictionaryAdapter(Converter<TK> keyConverter, Converter<TV> valueConverter)
        {
            this.keyConverter = keyConverter;
            this.valueConverter = valueConverter;
        }

        public sealed override Delegate GetToBytesDelegate() => new ToBytes<IEnumerable<KeyValuePair<TK, TV>>>(Bytes);

        public sealed override Delegate GetToValueDelegate() => new ToValue<Dictionary<TK, TV>>(Value);

        public sealed override Delegate GetToTupleDelegate() => new ToValue<List<Tuple<TK, TV>>>(Tuple);

        public void Bytes(Allocator allocator, IEnumerable<KeyValuePair<TK, TV>> value)
        {
            if (value == null)
                return;
            int offset;
            foreach (var i in value)
            {
                if (keyConverter.length == 0)
                {
                    offset = allocator.AnchorExtend();
                    keyConverter.ToBytes(allocator, i.Key);
                    allocator.FinishExtend(offset);
                }
                else
                {
                    keyConverter.ToBytes(allocator, i.Key);
                }
                if (valueConverter.length == 0)
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

        public unsafe Dictionary<TK, TV> Value(ReadOnlyMemory<byte> memory)
        {
            if (memory.IsEmpty)
                return new Dictionary<TK, TV>(0);
            var dictionary = new Dictionary<TK, TV>(8);
            fixed (byte* pointer = &memory.Span[0])
            {
                var vernier = new Vernier(pointer, memory.Length);
                while (vernier.Any())
                {
                    vernier.UpdateExcept(keyConverter.length);
                    var key = keyConverter.ToValue(memory.Slice(vernier.offset, vernier.length));
                    vernier.UpdateExcept(valueConverter.length);
                    var value = valueConverter.ToValue(memory.Slice(vernier.offset, vernier.length));
                    dictionary.Add(key, value);
                }
            }
            return dictionary;
        }

        public unsafe List<Tuple<TK, TV>> Tuple(ReadOnlyMemory<byte> memory)
        {
            if (memory.IsEmpty)
                return new List<Tuple<TK, TV>>(0);
            var list = new List<Tuple<TK, TV>>(8);
            fixed (byte* pointer = &memory.Span[0])
            {
                var vernier = new Vernier(pointer, memory.Length);
                while (vernier.Any())
                {
                    vernier.UpdateExcept(keyConverter.length);
                    var key = keyConverter.ToValue(memory.Slice(vernier.offset, vernier.length));
                    vernier.UpdateExcept(valueConverter.length);
                    var value = valueConverter.ToValue(memory.Slice(vernier.offset, vernier.length));
                    list.Add(new Tuple<TK, TV>(key, value));
                }
            }
            return list;
        }
    }
}
