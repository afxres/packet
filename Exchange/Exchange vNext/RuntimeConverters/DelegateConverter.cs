using System;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class DelegateConverter<T> : Converter<T>
    {
        private readonly ToBytes<T> toBytes;

        private readonly ToValue<T> toValue;

        public DelegateConverter(ToBytes<T> toBytes, ToValue<T> toValue, int length) : base(length)
        {
            this.toBytes = toBytes;
            this.toValue = toValue;
        }

        public override void ToBytes(ref Allocator allocator, T value) => toBytes.Invoke(ref allocator, value);

        public override T ToValue(ReadOnlySpan<byte> memory) => toValue.Invoke(memory);
    }
}
