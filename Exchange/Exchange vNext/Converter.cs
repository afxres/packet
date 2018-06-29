using System;
using System.Text;

namespace Mikodev.Binary
{
    public abstract class Converter
    {
        #region define
        public static readonly Encoding Encoding = Encoding.UTF8;

        public static readonly bool UseLittleEndian = true;
        #endregion

        internal int Length { get; }

        internal abstract Type ValueType { get; }

        internal Converter(int length)
        {
            if (length < 0)
                ThrowHelper.ThrowArgumentOutOfRange();
            Length = length;
        }

        internal abstract void ToBytesNonGeneric(Allocator allocator, object @object);

        internal abstract object ToValueNonGeneric(Block block);

        internal abstract Delegate ToBytesDelegate { get; }

        internal abstract Delegate ToValueDelegate { get; }

        #region override
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool Equals(object obj) => throw new InvalidOperationException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode() => throw new InvalidOperationException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override string ToString() => $"{nameof(Converter)} type : {ValueType}, byte length : {Length}";
        #endregion
    }

    public abstract class Converter<T> : Converter
    {
        internal sealed override Type ValueType => typeof(T);

        internal sealed override Delegate ToBytesDelegate { get; }

        internal sealed override Delegate ToValueDelegate { get; }

        protected Converter(int length) : base(length)
        {
            ToBytesDelegate = (Action<Allocator, T>)ToBytes;
            ToValueDelegate = (Func<Block, T>)ToValue;
        }

        public abstract void ToBytes(Allocator allocator, T value);

        public abstract T ToValue(Block block);

        internal sealed override void ToBytesNonGeneric(Allocator allocator, object @object) => ToBytes(allocator, (T)@object);

        internal sealed override object ToValueNonGeneric(Block block) => ToValue(block);
    }
}
