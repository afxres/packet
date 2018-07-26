using System;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class DelegateConverter<T> : Converter<T>, IDelegateConverter
    {
        private readonly Action<Allocator, T> toBytes;
        private readonly Func<ReadOnlyMemory<byte>, T> toValue;

        public Delegate ToBytesFunction => toBytes;
        public Delegate ToValueFunction => toValue;

        public DelegateConverter(Action<Allocator, T> toBytes, Func<ReadOnlyMemory<byte>, T> toValue, int length) : base(length)
        {
            this.toBytes = toBytes;
            this.toValue = toValue;
        }

        public override void ToBytes(Allocator allocator, T value) => toBytes.Invoke(allocator, value);

        public override T ToValue(ReadOnlyMemory<byte> memory) => toValue.Invoke(memory);
    }
}
