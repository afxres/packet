using System;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Mikodev.Binary
{
    public abstract class Converter
    {
        #region define
        public static readonly Encoding Encoding = Encoding.UTF8;
        public static readonly bool UseLittleEndian = true;
        #endregion

        #region private fields
        private readonly int length;
        private Cache cache;
        #endregion

        internal int Length => length;

        internal Cache Cache => cache;

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

        internal abstract MethodInfo GetToBytesMethodInfo();

        internal abstract MethodInfo GetToValueMethodInfo();

        public abstract void ToBytesAny(Allocator allocator, object value);

        public abstract object ToValueAny(ReadOnlyMemory<byte> memory);
    }

    public abstract class Converter<T> : Converter
    {
        protected Converter(int length) : base(length) { }

        internal sealed override MethodInfo GetToBytesMethodInfo() => new Action<Allocator, T>(ToBytes).GetMethodInfo();

        internal sealed override MethodInfo GetToValueMethodInfo() => new Func<ReadOnlyMemory<byte>, T>(ToValue).GetMethodInfo();

        public override void ToBytesAny(Allocator allocator, object value) => ToBytes(allocator, (T)value);

        public override object ToValueAny(ReadOnlyMemory<byte> memory) => ToValue(memory);

        public abstract void ToBytes(Allocator allocator, T value);

        public abstract T ToValue(ReadOnlyMemory<byte> memory);

        #region override
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public sealed override bool Equals(object obj) => throw new NotSupportedException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public sealed override int GetHashCode() => throw new NotSupportedException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public sealed override string ToString() => $"{nameof(Converter)}(Type: {typeof(T)}, Length: {Length})";
        #endregion
    }
}
