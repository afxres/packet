using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace Mikodev.Binary
{
    public abstract class Converter
    {
        private const int None = 0, Exchanged = 1, Initialized = 2;

        public static readonly Encoding Encoding = Encoding.UTF8;

        public static readonly bool UseLittleEndian = true;

        public int Length { get; }

        private int status = None;

        internal readonly Type type;

        internal Converter(Type type, int length)
        {
            if (length < 0)
                ThrowHelper.ThrowConverterLengthOutOfRange();
            Debug.Assert(type != null);
            this.type = type;
            Length = length;
        }

        internal abstract Delegate GetToBytesDelegate();

        internal abstract Delegate GetToValueDelegate();

        internal void Initialize(Cache cache)
        {
            if (Interlocked.CompareExchange(ref status, Exchanged, None) != None)
                ThrowHelper.ThrowConverterInitialized();
            OnInitialize(cache);
            var origin = Interlocked.Exchange(ref status, Initialized);
            Debug.Assert(origin == Exchanged);
        }

        protected virtual void OnInitialize(Cache cache) { }

        protected void EnsureInitialized()
        {
            if (status == Initialized)
                return;
            ThrowHelper.ThrowConverterNotInitialized();
        }

        public abstract void ToBytesAny(ref Allocator allocator, object value);

        public abstract object ToValueAny(ReadOnlySpan<byte> memory);

        #region override
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public sealed override bool Equals(object obj) => throw new NotSupportedException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public sealed override int GetHashCode() => throw new NotSupportedException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public sealed override string ToString() => $"{nameof(Converter)}(Type: {type}, Length: {Length})";
        #endregion
    }

    public abstract class Converter<T> : Converter
    {
        protected Converter(int length) : base(typeof(T), length) { }

        internal sealed override Delegate GetToBytesDelegate() => new ToBytes<T>(ToBytes);

        internal sealed override Delegate GetToValueDelegate() => new ToValue<T>(ToValue);

        public override void ToBytesAny(ref Allocator allocator, object value) => ToBytes(ref allocator, (T)value);

        public override object ToValueAny(ReadOnlySpan<byte> memory) => ToValue(memory);

        public abstract void ToBytes(ref Allocator allocator, T value);

        public abstract T ToValue(ReadOnlySpan<byte> memory);
    }
}
