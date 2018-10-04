using System;
using System.Collections.Generic;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class ExpandoConverter<T> : Converter<T>
    {
        private readonly ToBytes<T> toBytes;

        private readonly Func<Dictionary<string, ReadOnlyMemory<byte>>, T> toValue;

        private readonly int capacity;

        public ExpandoConverter(ToBytes<T> toBytes, Func<Dictionary<string, ReadOnlyMemory<byte>>, T> toValue, int capacity) : base(0)
        {
            this.toBytes = toBytes;
            this.toValue = toValue;
            this.capacity = capacity;
        }

        public override void ToBytes(Allocator allocator, T value)
        {
            if (value == null)
                return;
            toBytes.Invoke(allocator, value);
        }

        public override unsafe T ToValue(ReadOnlyMemory<byte> memory)
        {
            if (toValue == null)
                throw new InvalidOperationException($"Unable to get value, type: {typeof(T)}");

            if (memory.IsEmpty)
            {
                if (default(T) != null)
                    ThrowHelper.ThrowOverflow();
                return default;
            }

            var dictionary = new Dictionary<string, ReadOnlyMemory<byte>>(capacity);
            fixed (byte* pointer = &memory.Span[0])
            {
                var vernier = new Vernier(pointer, memory.Length);
                while (vernier.Any())
                {
                    vernier.Update();
                    var key = Encoding.GetString(pointer + vernier.offset, vernier.length);
                    vernier.Update();
                    var value = memory.Slice(vernier.offset, vernier.length);
                    dictionary.Add(key, value);
                }
            }
            return toValue.Invoke(dictionary);
        }
    }
}
