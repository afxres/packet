using Mikodev.Binary.Converters;
using Mikodev.Binary.RuntimeConverters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Mikodev.Binary
{
    public sealed partial class Cache
    {
        #region static
        private static readonly List<Converter> sharedConverters;
        private static readonly HashSet<Type> reserveTypes = new HashSet<Type>(typeof(Cache).Assembly.GetTypes());

        static Cache()
        {
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
            var converters = new List<Converter>(32);
            converters.AddRange(valueConverters);
            converters.AddRange(arrayConverters);
            converters.Add(new StringConverter());
            converters.Add(new DateTimeConverter());
            converters.Add(new TimeSpanConverter());
            converters.Add(new GuidConverter());
            converters.Add(new DecimalConverter());
            converters.Add(new IPAddressConverter());
            converters.Add(new IPEndPointConverter());
            sharedConverters = converters;
        }
        #endregion

        private readonly ConcurrentDictionary<Type, Converter> converters;
        private readonly ConcurrentDictionary<Type, Adapter> adapters = new ConcurrentDictionary<Type, Adapter>();
        private readonly ConcurrentDictionary<string, byte[]> encoding = new ConcurrentDictionary<string, byte[]>();

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

        internal Converter GetOrCreateConverter(Type type)
        {
            if (!converters.TryGetValue(type, out var converter))
                converters.TryAdd(type, (converter = CreateConverter(type)));
            return converter;
        }

        #region export
        private T Deserialize<T>(Block block)
        {
            var converter = (Converter<T>)GetOrCreateConverter(typeof(T));
            var value = converter.ToValue(block);
            return value;
        }

        private object Deserialize(Type type, Block block)
        {
            var converter = GetOrCreateConverter(type);
            var value = converter.ToValueAny(block);
            return value;
        }

        public T Deserialize<T>(byte[] buffer) => Deserialize<T>(new Block(buffer));

        public T Deserialize<T>(byte[] buffer, T anonymous) => Deserialize<T>(buffer);

        public T Deserialize<T>(byte[] buffer, int offset, int length) => Deserialize<T>(new Block(buffer, offset, length));

        public T Deserialize<T>(byte[] buffer, int offset, int length, T anonymous) => Deserialize<T>(buffer, offset, length);

        public object Deserialize(byte[] buffer, Type type) => Deserialize(type, new Block(buffer));

        public object Deserialize(byte[] buffer, int offset, int length, Type type) => Deserialize(type, new Block(buffer, offset, length));

        public byte[] Serialize<T>(T value)
        {
            var converter = (Converter<T>)GetOrCreateConverter(typeof(T));
            var stream = new UnsafeStream();
            var allocator = new Allocator(stream);
            converter.ToBytes(allocator, value);
            return stream.ToArray();
        }

        public byte[] Serialize(object value)
        {
            if (value == null)
                ThrowHelper.ThrowArgumentNull();
            var converter = GetOrCreateConverter(value.GetType());
            var stream = new UnsafeStream();
            var allocator = new Allocator(stream);
            converter.ToBytesAny(allocator, value);
            return stream.ToArray();
        }

        public Token NewToken(byte[] buffer) => new Token(this, new Block(buffer));

        public Token NewToken(byte[] buffer, int offset, int length) => new Token(this, new Block(buffer, offset, length));
        #endregion

        #region override
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool Equals(object obj) => throw new InvalidOperationException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode() => throw new InvalidOperationException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override string ToString() => $"{nameof(Cache)} converter count : {converters.Count}, encoding cache : {encoding.Count}";
        #endregion
    }
}
