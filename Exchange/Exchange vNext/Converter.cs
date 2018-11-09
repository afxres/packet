using System;
using System.Text;
using System.Threading;

namespace Mikodev.Binary
{
    public abstract class Converter
    {
        public static readonly Encoding Encoding = Encoding.UTF8;

        public static readonly bool UseLittleEndian = true;

        internal readonly int length;

        internal Cache cache;

        internal Converter(int length)
        {
            if (length < 0)
                ThrowHelper.ThrowArgumentOutOfRange();
            this.length = length;
        }

        internal void Initialize(Cache cache)
        {
            if (Interlocked.CompareExchange(ref this.cache, cache, null) == null)
                return;
            ThrowHelper.ThrowConverterInitialized();
        }

        protected Converter GetConverter(Type type)
        {
            if (type == null)
                ThrowHelper.ThrowArgumentNull();
            var cache = this.cache;
            if (cache == null)
                ThrowHelper.ThrowConverterNotInitialized();
            return cache.GetConverter(type);
        }

        protected Converter<T> GetConverter<T>() => (Converter<T>)GetConverter(typeof(T));

        internal abstract Type GetValueType();

        internal abstract Delegate GetToBytesDelegate();

        internal abstract Delegate GetToValueDelegate();

        public abstract void ToBytesAny(ref Allocator allocator, object value);

        public abstract object ToValueAny(ReadOnlySpan<byte> memory);

        #region override
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public sealed override bool Equals(object obj) => throw new NotSupportedException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public sealed override int GetHashCode() => throw new NotSupportedException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public sealed override string ToString() => $"{nameof(Converter)}(Type: {GetValueType()}, Length: {length})";
        #endregion
    }

    public abstract class Converter<T> : Converter
    {
        protected Converter(int length) : base(length) { }

        internal sealed override Type GetValueType() => typeof(T);

        internal sealed override Delegate GetToBytesDelegate() => new ToBytes<T>(ToBytes);

        internal sealed override Delegate GetToValueDelegate() => new ToValue<T>(ToValue);

        public override void ToBytesAny(ref Allocator allocator, object value) => ToBytes(ref allocator, (T)value);

        public override object ToValueAny(ReadOnlySpan<byte> memory) => ToValue(memory);

        public abstract void ToBytes(ref Allocator allocator, T value);

        public abstract T ToValue(ReadOnlySpan<byte> memory);
    }
}
