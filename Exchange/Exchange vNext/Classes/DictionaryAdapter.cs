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

        public void Bytes(ref Allocator allocator, IEnumerable<KeyValuePair<TK, TV>> value)
        {
            if (value == null)
                return;
            foreach (var i in value)
            {
                if (keyConverter.Length == 0)
                    allocator.AppendValueExtend(keyConverter, i.Key);
                else
                    keyConverter.ToBytes(ref allocator, i.Key);
                if (valueConverter.Length == 0)
                    allocator.AppendValueExtend(valueConverter, i.Value);
                else
                    valueConverter.ToBytes(ref allocator, i.Value);
            }
        }

        public unsafe Dictionary<TK, TV> Value(ReadOnlySpan<byte> memory)
        {
            if (memory.IsEmpty)
                return new Dictionary<TK, TV>(0);
            var dictionary = new Dictionary<TK, TV>(8);
            fixed (byte* srcptr = memory)
            {
                var vernier = new Vernier(srcptr, memory.Length);
                while (vernier.Any())
                {
                    vernier.UpdateExcept(keyConverter.Length);
                    var key = keyConverter.ToValue(memory.Slice(vernier.offset, vernier.length));
                    vernier.UpdateExcept(valueConverter.Length);
                    var value = valueConverter.ToValue(memory.Slice(vernier.offset, vernier.length));
                    dictionary.Add(key, value);
                }
            }
            return dictionary;
        }

        public unsafe List<Tuple<TK, TV>> Tuple(ReadOnlySpan<byte> memory)
        {
            if (memory.IsEmpty)
                return new List<Tuple<TK, TV>>(0);
            var list = new List<Tuple<TK, TV>>(8);
            fixed (byte* srcptr = memory)
            {
                var vernier = new Vernier(srcptr, memory.Length);
                while (vernier.Any())
                {
                    vernier.UpdateExcept(keyConverter.Length);
                    var key = keyConverter.ToValue(memory.Slice(vernier.offset, vernier.length));
                    vernier.UpdateExcept(valueConverter.Length);
                    var value = valueConverter.ToValue(memory.Slice(vernier.offset, vernier.length));
                    list.Add(new Tuple<TK, TV>(key, value));
                }
            }
            return list;
        }
    }
}
