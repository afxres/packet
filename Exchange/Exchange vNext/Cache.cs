using Mikodev.Binary.Converters;
using Mikodev.Binary.RuntimeConverters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary
{
    public sealed partial class Cache
    {
        #region static
        private static readonly List<Converter> sharedConverters;
        private static readonly HashSet<Type> reserveTypes = new HashSet<Type>(typeof(Cache).Assembly.GetTypes());

        static Cache()
        {
            var converters = new List<Converter>(32)
            {
                new StringConverter(),
                new DateTimeConverter(),
                new TimeSpanConverter(),
                new GuidConverter(),
                new DecimalConverter(),
                new IPAddressConverter(),
                new IPEndPointConverter()
            };
            var unmanagedTypes = new[]
            {
                typeof(bool),
                typeof(byte),
                typeof(sbyte),
                typeof(char),
                typeof(short),
                typeof(int),
                typeof(long),
                typeof(ushort),
                typeof(uint),
                typeof(ulong),
                typeof(float),
                typeof(double),
            };
            var valueConverters = unmanagedTypes.Select(r => (Converter)Activator.CreateInstance(typeof(UnmanagedValueConverter<>).MakeGenericType(r)));
            var arrayConverters = unmanagedTypes.Select(r => (Converter)Activator.CreateInstance(typeof(UnmanagedArrayConverter<>).MakeGenericType(r)));
            converters.AddRange(valueConverters);
            converters.AddRange(arrayConverters);
            sharedConverters = converters;
        }
        #endregion

        private readonly ConcurrentDictionary<Type, Converter> converters;
        private readonly ConcurrentDictionary<Type, DictionaryAdapter> adapters = new ConcurrentDictionary<Type, DictionaryAdapter>();
        private readonly ConcurrentDictionary<string, byte[]> texts = new ConcurrentDictionary<string, byte[]>();

        public Cache(IEnumerable<Converter> converters = null)
        {
            var dictionary = new ConcurrentDictionary<Type, Converter>();
            if (converters != null)
                foreach (var i in converters)
                    dictionary.TryAdd(i.ValueType, i);
            foreach (var i in sharedConverters)
                dictionary.TryAdd(i.ValueType, i);
            dictionary[typeof(object)] = new ObjectConverter(this);
            this.converters = dictionary;
        }

        internal Converter GetConverter(Type type)
        {
            if (!converters.TryGetValue(type, out var converter))
                converter = ConverterGenerator.GenerateConverter(this, type);
            return converter;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Converter<T> GetConverter<T>() => (Converter<T>)GetConverter(typeof(T));

        #region deserialize
        public T ToValue<T>(Block block)
        {
            var converter = GetConverter<T>();
            var value = converter.ToValue(block);
            return value;
        }

        public object ToValue(Block block, Type type)
        {
            if (type == null)
                ThrowHelper.ThrowArgumentNull();
            var converter = GetConverter(type);
            var value = converter.ToValueAny(block);
            return value;
        }

        public object ToValue(byte[] bytes, Type type) => ToValue(new Block(bytes), type);

        public T ToValue<T>(byte[] bytes) => ToValue<T>(new Block(bytes));

        public T ToValue<T>(Block block, T anonymous) => ToValue<T>(block);

        public T ToValue<T>(byte[] bytes, T anonymous) => ToValue<T>(new Block(bytes));
        #endregion

        #region serialize, token
        public byte[] ToBytes<T>(T value)
        {
            var converter = GetConverter<T>();
            var stream = new UnsafeStream();
            var allocator = new Allocator(stream);
            converter.ToBytes(allocator, value);
            return stream.ToArray();
        }

        public byte[] ToBytes(object value)
        {
            if (value == null)
                ThrowHelper.ThrowArgumentNull();
            var converter = GetConverter(value.GetType());
            var stream = new UnsafeStream();
            var allocator = new Allocator(stream);
            converter.ToBytesAny(allocator, value);
            return stream.ToArray();
        }

        public Token AsToken(Block block) => new Token(this, block);

        public Token AsToken(byte[] bytes) => AsToken(new Block(bytes));
        #endregion

        #region override
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool Equals(object obj) => throw new InvalidOperationException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode() => throw new InvalidOperationException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override string ToString() => $"{nameof(Cache)} converter count : {converters.Count}, encoding cache : {texts.Count}";
        #endregion
    }
}
