using System;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class FixedConverter<T> : Converter<T>
    {
        private readonly ToBytes<T> toBytes;

        private readonly ToValueFixed<T> toValue;

        public FixedConverter(ToBytes<T> toBytes, ToValueFixed<T> toValue, int length) : base(length)
        {
            this.toBytes = toBytes;
            this.toValue = toValue;
        }

        public override void ToBytes(Allocator allocator, T value) => toBytes.Invoke(allocator, value);

        public override unsafe T ToValue(ReadOnlyMemory<byte> memory)
        {
            if (memory.IsEmpty)
                ThrowHelper.ThrowOverflow();
            fixed (byte* pointer = &memory.Span[0])
                return toValue.Invoke(memory, new Vernier(pointer, memory.Length));
        }
    }
}
