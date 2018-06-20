using System;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class DelegateConverter<T> : Converter<T>
    {
        private readonly Action<Allocator, T> toBytes;
        private readonly Func<Block, T> toValue;

        public DelegateConverter(Action<Allocator, T> toBytes, Func<Block, T> toValue, int length) : base(length)
        {
            this.toBytes = toBytes;
            this.toValue = toValue;
        }

        public override void ToBytes(Allocator allocator, T value) => toBytes.Invoke(allocator, value);

        public override T ToValue(Block block) => toValue.Invoke(block);
    }
}
