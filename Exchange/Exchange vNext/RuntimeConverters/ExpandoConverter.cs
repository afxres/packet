using Mikodev.Binary.Common;
using System;
using System.Collections.Generic;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class ExpandoConverter<T> : Converter<T>
    {
        private readonly Action<Allocator, T> toBytes;
        private readonly Func<Dictionary<string, Block>, T> toValue;
        private readonly int toValueCapacity;

        public ExpandoConverter(Action<Allocator, T> toBytes, Func<Dictionary<string, Block>, T> toValue, int toValueCapacity) : base(0)
        {
            this.toBytes = toBytes;
            this.toValue = toValue;
            this.toValueCapacity = toValueCapacity;
        }

        public override void ToBytes(Allocator allocator, T value) => toBytes.Invoke(allocator, value);

        public override T ToValue(Block block)
        {
            if (toValue == null)
                throw new InvalidOperationException();
            var vernier = (Vernier)block;
            var dictionary = new Dictionary<string, Block>(toValueCapacity);
            while (vernier.Any)
            {
                vernier.Flush();
                var key = Extension.Encoding.GetString(vernier.Buffer, vernier.Offset, vernier.Length);
                vernier.Flush();
                dictionary.Add(key, (Block)vernier);
            }
            return toValue.Invoke(dictionary);
        }
    }
}
