using System;
using System.Collections.Generic;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class ExpandoConverter<T> : Converter<T>
    {
        private readonly Action<Allocator, T> toBytes;
        private readonly Func<Dictionary<string, Block>, T> toValue;
        private readonly int capacity;

        public ExpandoConverter(Action<Allocator, T> toBytes, Func<Dictionary<string, Block>, T> toValue, int capacity) : base(0)
        {
            this.toBytes = toBytes;
            this.toValue = toValue;
            this.capacity = capacity;
        }

        public override void ToBytes(Allocator allocator, T value) => toBytes.Invoke(allocator, value);

        public override T ToValue(Block block)
        {
            if (toValue == null)
                throw new InvalidOperationException($"Unable to get value, type : {typeof(T)}");
            var vernier = (Vernier)block;
            var dictionary = new Dictionary<string, Block>(capacity);
            while (vernier.Any)
            {
                vernier.Flush();
                var key = Encoding.GetString(vernier.Buffer, vernier.Offset, vernier.Length);
                vernier.Flush();
                dictionary.Add(key, (Block)vernier);
            }
            return toValue.Invoke(dictionary);
        }
    }
}
