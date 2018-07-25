using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class ExpandoConverter<T> : Converter<T>
    {
        private readonly Action<Allocator, T> toBytes;
        private readonly Func<Dictionary<string, Memory<byte>>, T> toValue;
        private readonly int capacity;

        public ExpandoConverter(Action<Allocator, T> toBytes, Func<Dictionary<string, Memory<byte>>, T> toValue, int capacity) : base(0)
        {
            this.toBytes = toBytes;
            this.toValue = toValue;
            this.capacity = capacity;
        }

        public override void ToBytes(Allocator allocator, T value) => toBytes.Invoke(allocator, value);

        public override T ToValue(Memory<byte> memory)
        {
            if (toValue == null)
                throw new InvalidOperationException($"Unable to get value, type : {typeof(T)}");
            var dictionary = new Dictionary<string, Memory<byte>>(capacity);
            var span = memory.Span;
            var vernier = (Vernier)memory;
            while (vernier.Any())
            {
                vernier.Flush();
                var key = Encoding.GetString(ref span[vernier.offset], vernier.length);
                vernier.Flush();
                dictionary.Add(key, (Memory<byte>)vernier);
            }
            return toValue.Invoke(dictionary);
        }
    }
}
