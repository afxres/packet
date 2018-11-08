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
        private static readonly Dictionary<Type, Type> converterTypes;

        static Cache()
        {
            Type identifier(Type type)
            {
                while ((type = type.BaseType) != null)
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Converter<>))
                        return type.GetGenericArguments().Single();
                throw new ApplicationException();
            }

            var types = new[]
            {
                typeof(StringConverter),
                typeof(DateTimeConverter),
                typeof(TimeSpanConverter),
                typeof(GuidConverter),
                typeof(DecimalConverter),
                typeof(IPAddressConverter),
                typeof(IPEndPointConverter)
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

            var dictionary = types.ToDictionary(identifier);
            foreach (var type in unmanagedTypes)
                dictionary.Add(type, typeof(UnmanagedValueConverter<>).MakeGenericType(type));
            foreach (var type in unmanagedTypes)
                dictionary.Add(type.MakeArrayType(), typeof(UnmanagedArrayConverter<>).MakeGenericType(type));
            converterTypes = dictionary;
        }

        private static ConcurrentDictionary<Type, Converter> GetConverters(IEnumerable<Converter> converters)
        {
            var dictionary = new ConcurrentDictionary<Type, Converter>();
            // add user-defined converters
            if (converters != null)
                foreach (var i in converters)
                    if (i != null)
                        dictionary.TryAdd(i.GetValueType(), i);
            // try add converters
            foreach (var i in converterTypes)
                if (!dictionary.ContainsKey(i.Key))
                    dictionary.TryAdd(i.Key, (Converter)Activator.CreateInstance(i.Value));
            // set object converter
            dictionary[typeof(object)] = new ObjectConverter();
            return dictionary;
        }
        #endregion

        private readonly ConcurrentDictionary<Type, Converter> converters;

        private readonly ConcurrentDictionary<Type, DictionaryAdapter> adapters = new ConcurrentDictionary<Type, DictionaryAdapter>();

        private readonly ConcurrentDictionary<string, byte[]> texts = new ConcurrentDictionary<string, byte[]>();

        public Cache(IEnumerable<Converter> converters = null)
        {
            var dictionary = GetConverters(converters);
            foreach (var converter in dictionary.Values)
                converter.Initialize(this);
            this.converters = dictionary;
        }

        internal Converter GetConverter(Type type)
        {
            if (converters.TryGetValue(type, out var result))
                return result;
            var generator = new ConverterGenerator(this);
            return generator.GetOrGenerateConverter(type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Converter<T> GetConverter<T>() => (Converter<T>)GetConverter(typeof(T));

        #region deserialize
        public T ToValue<T>(ReadOnlySpan<byte> memory)
        {
            var converter = GetConverter<T>();
            var value = converter.ToValue(memory);
            return value;
        }

        public object ToValue(ReadOnlySpan<byte> memory, Type type)
        {
            if (type == null)
                ThrowHelper.ThrowArgumentNull();
            var converter = GetConverter(type);
            var value = converter.ToValueAny(memory);
            return value;
        }

        public T ToValue<T>(ReadOnlySpan<byte> memory, T anonymous) => ToValue<T>(memory);
        #endregion

        #region serialize, token
        public byte[] ToBytes<T>(T value)
        {
            var converter = GetConverter<T>();
            var allocator = new Allocator();
            converter.ToBytes(allocator, value);
            return allocator.ToArray();
        }

        public byte[] ToBytes(object value)
        {
            if (value == null)
                ThrowHelper.ThrowArgumentNull();
            var converter = GetConverter(value.GetType());
            var allocator = new Allocator();
            converter.ToBytesAny(allocator, value);
            return allocator.ToArray();
        }

        public Token AsToken(ReadOnlyMemory<byte> memory) => new Token(this, memory);
        #endregion

        #region override
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public sealed override bool Equals(object obj) => throw new NotSupportedException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public sealed override int GetHashCode() => throw new NotSupportedException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public sealed override string ToString() => $"{nameof(Cache)}(Converters: {converters.Count})";
        #endregion
    }
}
