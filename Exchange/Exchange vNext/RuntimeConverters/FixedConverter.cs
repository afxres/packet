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

        public override void ToBytes(Allocator allocator, T value)
        {
            if (value == null)
                return;
            toBytes.Invoke(allocator, value);
        }

        public override unsafe T ToValue(ReadOnlyMemory<byte> memory)
        {
            if (memory.IsEmpty)
                return ThrowHelper.ThrowOverflowOrNull<T>();
            fixed (byte* srcptr = memory.Span)
                return toValue.Invoke(memory, new Vernier(srcptr, memory.Length));
        }
    }
}
