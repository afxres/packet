using Mikodev.Binary.Common;
using Mikodev.Binary.Converters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Mikodev.Binary
{
    public class PacketBuilder
    {
        private readonly Dictionary<Type, ValueConverter> valueConverters;

        private readonly ConcurrentDictionary<Type, Delegate> delegates = new ConcurrentDictionary<Type, Delegate>();

        private readonly ConcurrentDictionary<string, byte[]> stringCache = new ConcurrentDictionary<string, byte[]>();

        public static PacketBuilder Default { get; }

        static PacketBuilder()
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
            Default = new PacketBuilder(converters);
        }

        public PacketBuilder(IEnumerable<Converter> converters)
        {
            if (converters is null)
                throw new ArgumentNullException(nameof(converters));
            valueConverters = converters.OfType<ValueConverter>().ToDictionary(r => r.ValueType);
        }

        #region static
        private static readonly MethodInfo GetToBytesExpressionMethodInfo = typeof(PacketBuilder).GetMethod(nameof(GetToBytesExpression), BindingFlags.Instance | BindingFlags.NonPublic);

        private static Expression<Action<Allocator, T>> GetToBytesExpression<T>(ValueConverter converter)
        {
            var generic = (ValueConverter<T>)converter;
            return (allocator, value) => generic.ToBytes(allocator, value);
        }

        private static LambdaExpression GetToBytesExpression(Type type, ValueConverter valueConverter)
        {
            return (LambdaExpression)GetToBytesExpressionMethodInfo.MakeGenericMethod(type).Invoke(null, new object[] { valueConverter });
        }

        private static readonly MethodInfo WriteExtendMethodInfo = typeof(Allocator).GetMethod(nameof(Allocator.WriteExtend), BindingFlags.Instance | BindingFlags.NonPublic);
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

        private byte[] GetOrCache(string key)
        {
            if (!stringCache.TryGetValue(key, out var bytes))
                stringCache.TryAdd(key, (bytes = (MemoryMarshal.Cast<char, byte>(key.AsSpan())).ToArray()));
            return bytes;
        }

        private Delegate CreateDelegate(Type type)
        {
            if (valueConverters.TryGetValue(type, out var valueConverter))
                return GetToBytesExpression(type, valueConverter).Compile();
            var properties = type.GetProperties();

            var instance = Expression.Parameter(type, "instance");
            var allocator = Expression.Parameter(typeof(Allocator), "allocator");
            var list = new List<Expression>();
            foreach (var i in properties)
            {
                var getMethod = i.GetGetMethod();
                if (getMethod == null || getMethod.GetParameters().Length != 0)
                    continue;
                var propertyType = i.PropertyType;
                var buffer = GetOrCache(i.Name);
                list.Add(Expression.Call(allocator, WriteExtendMethodInfo, Expression.Constant(buffer)));
                var propertyValue = Expression.Call(instance, getMethod);
                if (valueConverters.TryGetValue(propertyType, out var converter))
                {
                    var converterType = typeof(ValueConverter<>).MakeGenericType(propertyType);
                    var toBytesExtend = converterType.GetMethod("ToBytesExtend", BindingFlags.Instance | BindingFlags.NonPublic);
                    list.Add(Expression.Call(Expression.Constant(converter, converterType), toBytesExtend, allocator, propertyValue));
                }
                else
                {
                    var @delegate = CreateDelegate(propertyType);
                    var delegateType = typeof(Action<,>).MakeGenericType(typeof(Allocator), propertyType);
                    list.Add(Expression.Call(Expression.Constant(@delegate, delegateType), delegateType.GetMethod("Invoke"), allocator, propertyValue));
                }
            }
            var block = Expression.Block(list);
            var expression = Expression.Lambda(block, allocator, instance);
            return expression.Compile();
        }

        public byte[] Serialize<T>(T value)
        {
            var allocator = new Allocator();
            var function = (Action<Allocator, T>)GetOrCreateDelegate(typeof(T));
            function.Invoke(allocator, value);
            return allocator.GetBytes();
        }
    }
}
