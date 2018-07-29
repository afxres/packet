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
        internal abstract Delegate ToBytesDelegate { get; }
        internal abstract Delegate ToValueDelegate { get; }

        internal Converter(int length)
        {
            if (length < 0)
                ThrowHelper.ThrowArgumentOutOfRange();
            Length = length;
        }

        internal abstract void ToBytesAny(Allocator allocator, object value);

        internal abstract object ToValueAny(ReadOnlyMemory<byte> memory);

        #region override
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool Equals(object obj) => throw new InvalidOperationException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode() => throw new InvalidOperationException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override string ToString() => $"{nameof(Converter)} type: {ValueType}, byte length : {Length}";
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
            ToValueDelegate = (Func<ReadOnlyMemory<byte>, T>)ToValue;
        }

        public abstract void ToBytes(Allocator allocator, T value);

        public abstract T ToValue(ReadOnlyMemory<byte> memory);

        internal sealed override void ToBytesAny(Allocator allocator, object value) => ToBytes(allocator, (T)value);

        internal sealed override object ToValueAny(ReadOnlyMemory<byte> memory) => ToValue(memory);
    }
}
