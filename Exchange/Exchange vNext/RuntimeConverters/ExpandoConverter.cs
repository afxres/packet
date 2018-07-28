using System;
using System.Collections.Generic;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class ExpandoConverter<T> : Converter<T>
    {
        private readonly Action<Allocator, T> toBytes;
        private readonly Func<Dictionary<string, ReadOnlyMemory<byte>>, T> toValue;
        private readonly int capacity;

        public ExpandoConverter(Action<Allocator, T> toBytes, Func<Dictionary<string, ReadOnlyMemory<byte>>, T> toValue, int capacity) : base(0)
        {
            this.toBytes = toBytes;
            this.toValue = toValue;
            this.capacity = capacity;
        }

        public override void ToBytes(Allocator allocator, T value) => toBytes.Invoke(allocator, value);

        public override T ToValue(ReadOnlyMemory<byte> memory)
        {
            if (toValue == null)
                throw new InvalidOperationException($"Unable to get value, type : {typeof(T)}");
            if (capacity == 0)
            {
                if (!memory.IsEmpty)
                    throw new InvalidOperationException("Memory is not empty!");
                return toValue.Invoke(new Dictionary<string, ReadOnlyMemory<byte>>(0));
            }
            var dictionary = new Dictionary<string, ReadOnlyMemory<byte>>(capacity);
            var span = memory.Span;
            ref readonly var location = ref span[0];
            var vernier = new Vernier(span.Length);
            while (vernier.Any())
            {
                vernier.Flush(in location);
                var key = Encoding.GetString(in span[vernier.offset], vernier.length);
                vernier.Flush(in location);
                dictionary.Add(key, memory.Slice(vernier.offset, vernier.length));
            }
            return toValue.Invoke(dictionary);
        }
    }
}
