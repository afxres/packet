using System;

namespace Mikodev.Binary.Common
{
    public abstract class ValueConverter : Converter
    {
        internal int Length { get; }

        internal abstract Type ValueType { get; }

        internal ValueConverter(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            Length = length;
        }

        internal abstract void ToBytesNonGeneric(Allocator allocator, object @object);

        internal abstract object ToValueNonGeneric(Block block);
    }

    public abstract class ValueConverter<T> : ValueConverter
    {
        internal sealed override Type ValueType => typeof(T);

        protected ValueConverter(int length) : base(length) { }

        public abstract T ToValue(Block block);

        public abstract void ToBytes(Allocator allocator, T value);

        internal sealed override object ToValueNonGeneric(Block block) => ToValue(block);

        internal sealed override void ToBytesNonGeneric(Allocator allocator, object @object) => ToBytes(allocator, (T)@object);

        internal void ToBytesExtend(Allocator allocator, T value)
        {
            var offset = allocator.stream.BeginModify();
            ToBytes(allocator, value);
            allocator.stream.EndModify(offset);
        }
    }
}
