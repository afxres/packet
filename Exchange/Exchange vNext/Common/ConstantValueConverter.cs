using System;

namespace Mikodev.Binary.Common
{
    public abstract class ConstantValueConverter : ValueConverter
    {
        public int Length { get; }

        protected ConstantValueConverter(int length)
        {
            if (length < 1)
                throw new ArgumentOutOfRangeException(nameof(length));
            Length = length;
        }
    }

    public abstract class ConstantValueConverter<T> : ConstantValueConverter
    {
        internal sealed override Type ValueType => typeof(T);

        public ConstantValueConverter(int length) : base(length) { }

        public abstract void ToBytes(Span<byte> block, T value);

        public abstract T ToValue(Span<byte> block);
    }
}
