using System;

namespace Mikodev.Binary
{
    public abstract class Converter
    {
        internal int Length { get; }

        internal abstract Type ValueType { get; }

        internal Converter(int length)
        {
            if (length < 0)
                throw new ArgumentOutOfRangeException(nameof(length));
            Length = length;
        }

        internal abstract void ToBytesNonGeneric(Allocator allocator, object @object);

        internal abstract object ToValueNonGeneric(Block block);

        internal abstract Delegate ToBytesDelegate { get; }

        internal abstract Delegate ToValueDelegate { get; }
    }

    public abstract class Converter<T> : Converter
    {
        internal sealed override Type ValueType => typeof(T);

        protected Converter(int length) : base(length)
        {
            ToBytesDelegate = (Action<Allocator, T>)ToBytes;
            ToValueDelegate = (Func<Block, T>)ToValue;
        }

        public abstract void ToBytes(Allocator allocator, T value);

        public abstract T ToValue(Block block);

        internal sealed override void ToBytesNonGeneric(Allocator allocator, object @object) => ToBytes(allocator, (T)@object);

        internal sealed override object ToValueNonGeneric(Block block) => ToValue(block);

        internal sealed override Delegate ToBytesDelegate { get; }

        internal sealed override Delegate ToValueDelegate { get; }
    }
}
