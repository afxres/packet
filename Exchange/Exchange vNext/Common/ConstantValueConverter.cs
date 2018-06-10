using System;

namespace Mikodev.Binary.Common
{
    public abstract class ConstantValueConverter<T> : ValueConverter<T>
    {
        internal sealed override Type ValueType => typeof(T);

        protected ConstantValueConverter(int length) : base(length)
        {
            if (length > 0)
                return;
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        public abstract void ToBytes(Span<byte> block, T value);

        internal sealed override void ToBytes(Allocator allocator, object @object)
        {
            var value = (T)@object;
            var block = allocator.Allocate(Length);
            ToBytes(block, value);
        }
    }
}
