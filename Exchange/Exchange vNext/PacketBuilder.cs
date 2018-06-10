using Mikodev.Binary.Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mikodev.Binary
{
    public class PacketBuilder
    {
        private delegate void SerializeFunction<in T>(UnsafeStream stream, T value);

        private readonly Dictionary<Type, ValueConverter> valueConverters;
        private readonly Dictionary<Type, ArrayConverter> arrayConverters;

        private readonly ConcurrentDictionary<Type, Delegate> delegates = new ConcurrentDictionary<Type, Delegate>();

        public PacketBuilder(IEnumerable<Converter> converters)
        {
            if (converters is null)
                throw new ArgumentNullException(nameof(converters));
            valueConverters = converters.OfType<ValueConverter>().ToDictionary(r => r.ValueType);
            arrayConverters = converters.OfType<ArrayConverter>().ToDictionary(r => r.ArrayType);
        }

        #region static
        private static readonly MethodInfo GetToBytesExpressionMethodInfo = typeof(PacketBuilder).GetMethod(nameof(GetToBytesExpression), BindingFlags.Instance | BindingFlags.NonPublic);

        private static Expression<SerializeFunction<T>> GetToBytesExpression<T>(ValueConverter converter)
        {
            switch (converter)
            {
                case ConstantValueConverter<T> constant:
                    return (stream, value) => constant.ToBytes(stream.Allocate(constant.Length), value);
                case VariableValueConverter<T> variable:
                    return (stream, value) => variable.ToBytes(new Allocator(stream), value);
                default:
                    throw new PacketException($"Invalid {nameof(ValueConverter)} : {converter.GetType()}");
            }
        }

        private static LambdaExpression GetToBytesExpression(Type type, ValueConverter valueConverter)
        {
            return (LambdaExpression)GetToBytesExpressionMethodInfo.MakeGenericMethod(type).Invoke(null, new object[] { valueConverter });
        }
        #endregion

        private Delegate GetOrCreateDelegate(Type type)
        {
            if (!delegates.TryGetValue(type, out var @delegate))
            {
                @delegate = CreateDelegate(type);
                delegates.TryAdd(type, @delegate);
            }
            return @delegate;
        }

        private Delegate CreateDelegate(Type type)
        {
            if (valueConverters.TryGetValue(type, out var valueConverter))
                return GetToBytesExpression(type, valueConverter).Compile();
            throw new NotImplementedException();
        }

        public byte[] Serialize<T>(T value)
        {
            var stream = new UnsafeStream();
            var function = (SerializeFunction<T>)GetOrCreateDelegate(typeof(T));
            function.Invoke(stream, value);
            return stream.GetBytes();
        }
    }
}
