using Mikodev.Binary.Common;
using Mikodev.Binary.Converters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace Mikodev.Binary
{
    public class PacketCache
    {
        private static readonly IReadOnlyList<Converter> defaultConverters;

        private readonly Dictionary<Type, ValueConverter> valueConverters;
        private readonly ConcurrentDictionary<Type, Delegate> delegates = new ConcurrentDictionary<Type, Delegate>();
        private readonly ConcurrentDictionary<string, byte[]> stringCache = new ConcurrentDictionary<string, byte[]>();

        static PacketCache()
        {
            var unmanagedTypes = new[]
            {
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
            var valueConverters = unmanagedTypes.Select(r => (ValueConverter)Activator.CreateInstance(typeof(UnmanagedValueConverter<>).MakeGenericType(r)));
            var arrayConverters = unmanagedTypes.Select(r => (ValueConverter)Activator.CreateInstance(typeof(UnmanagedArrayConverter<>).MakeGenericType(r)));
            var converters = new List<Converter>();
            converters.AddRange(valueConverters);
            converters.AddRange(arrayConverters);
            converters.Add(new StringConverter());
            defaultConverters = converters;
        }

        public PacketCache(IEnumerable<Converter> converters = null)
        {
            var dictionary = defaultConverters.OfType<ValueConverter>().ToDictionary(r => r.ValueType);
            if (converters != null)
                foreach (var i in converters.OfType<ValueConverter>())
                    dictionary[i.ValueType] = i;
            valueConverters = dictionary;
        }

        private Delegate GetOrCreateToBytesDelegate(Type type)
        {
            if (!delegates.TryGetValue(type, out var @delegate))
                delegates.TryAdd(type, (@delegate = ToBytesDelegate(type)));
            return @delegate;
        }

        private byte[] GetOrCache(string key)
        {
            if (!stringCache.TryGetValue(key, out var bytes))
                stringCache.TryAdd(key, (bytes = Encoding.UTF8.GetBytes(key)));
            return bytes;
        }

        private Delegate ToBytesDelegate(Type type)
        {
            if (valueConverters.TryGetValue(type, out var valueConverter))
                return Convert.ValueToBytesExpression(type, valueConverter).Compile();
            if (type.IsArray)
            {
                if (type.GetArrayRank() != 1)
                    throw new NotSupportedException("Multidimensional arrays are not supported, use array of arrays instead.");
                var elementType = type.GetElementType();
                if (elementType == typeof(object))
                    goto fail;
                var @delegate = valueConverters.TryGetValue(elementType, out var converter)
                    ? null
                    : GetOrCreateToBytesDelegate(elementType);
                var expression = Convert.ArrayToBytesLambdaExpression(elementType, converter, @delegate);
                return expression.Compile();
            }

            var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
            if (definition == typeof(List<>))
            {
                var elementType = type.GetGenericArguments().Single();
                if (elementType == typeof(object))
                    goto fail;
                var @delegate = valueConverters.TryGetValue(elementType, out var converter)
                    ? null
                    : GetOrCreateToBytesDelegate(elementType);
                var expression = Convert.ListToBytesLambdaExpression(elementType, converter, @delegate);
                return expression.Compile();
            }
            return ToBytesDelegateFromProperties(type);

            fail:
            throw new InvalidOperationException($"Invalid collection type: {type}");
        }

        private Delegate ToBytesDelegateFromProperties(Type type)
        {
            var properties = type.GetProperties();
            var instance = Expression.Parameter(type, "instance");
            var allocator = Expression.Parameter(typeof(Allocator), "allocator");
            var stream = Expression.Field(allocator, nameof(Allocator.stream));
            var list = new List<Expression>();
            var position = default(ParameterExpression);
            var variableList = new List<ParameterExpression>();
            foreach (var i in properties)
            {
                var getMethod = i.GetGetMethod();
                if (getMethod == null || getMethod.GetParameters().Length != 0)
                    continue;
                var propertyType = i.PropertyType;
                var buffer = GetOrCache(i.Name);
                list.Add(Expression.Call(stream, UnsafeStream.WriteExtendMethodInfo, Expression.Constant(buffer)));
                var propertyValue = Expression.Call(instance, getMethod);
                if (valueConverters.TryGetValue(propertyType, out var converter))
                {
                    var converterType = typeof(ValueConverter<>).MakeGenericType(propertyType);
                    var toBytesExtend = converterType.GetMethod("ToBytesExtend", BindingFlags.Instance | BindingFlags.NonPublic);
                    list.Add(Expression.Call(Expression.Constant(converter, converterType), toBytesExtend, allocator, propertyValue));
                }
                else
                {
                    var @delegate = GetOrCreateToBytesDelegate(propertyType);
                    var delegateType = typeof(Action<,>).MakeGenericType(typeof(Allocator), propertyType);
                    if (position == null)
                        variableList.Add(position = Expression.Variable(typeof(int), "position"));
                    list.Add(Expression.Assign(position, Expression.Call(stream, UnsafeStream.BeginModifyMethodInfo)));
                    list.Add(Expression.Call(Expression.Constant(@delegate, delegateType), delegateType.GetMethod("Invoke"), allocator, propertyValue));
                    list.Add(Expression.Call(stream, UnsafeStream.EndModifyMethodInfo, position));
                }
            }
            var block = Expression.Block(variableList, list);
            var expression = Expression.Lambda(block, allocator, instance);
            return expression.Compile();
        }

        public byte[] Serialize<T>(T value)
        {
            var stream = new UnsafeStream();
            var allocator = new Allocator(stream);
            var function = (Action<Allocator, T>)GetOrCreateToBytesDelegate(typeof(T));
            function.Invoke(allocator, value);
            return stream.GetBytes();
        }
    }
}
