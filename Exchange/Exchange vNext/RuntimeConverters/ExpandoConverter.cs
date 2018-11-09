using System;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class ExpandoConverter<T> : Converter<T>
    {
        private readonly ToBytes<T> toBytes;

        private readonly ToValueExpando<T> toValue;

        private readonly int capacity;

        public ExpandoConverter(ToBytes<T> toBytes, ToValueExpando<T> toValue, int capacity) : base(0)
        {
            this.toBytes = toBytes;
            this.toValue = toValue;
            this.capacity = capacity;
        }

        public override void ToBytes(ref Allocator allocator, T value)
        {
            if (value == null)
                return;
            toBytes.Invoke(ref allocator, value);
        }

        public override unsafe T ToValue(ReadOnlySpan<byte> memory)
        {
            if (toValue == null)
                throw new InvalidOperationException($"Unable to get value, type: {typeof(T)}");
            if (memory.IsEmpty)
                return ThrowHelper.ThrowOverflowOrNull<T>();

            var dictionary = new HybridDictionary(capacity);
            fixed (byte* srcptr = memory)
            {
                var vernier = new Vernier(srcptr, memory.Length);
                while (vernier.Any())
                {
                    vernier.Update();
                    var key = Encoding.GetString(srcptr + vernier.offset, vernier.length);
                    vernier.Update();
                    var value = new Segment(vernier.offset, vernier.length);
                    dictionary.Add(key, value);
                }
            }
            return toValue.Invoke(memory, dictionary);
        }
    }
}
