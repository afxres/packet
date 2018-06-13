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

        private static readonly MethodInfo WriteExtendMethodInfo = typeof(Allocator).GetMethod(nameof(Allocator.WriteExtend), BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo BeginModifyMethodInfo = typeof(Allocator).GetMethod(nameof(Allocator.BeginModify), BindingFlags.Instance | BindingFlags.NonPublic);

        private static readonly MethodInfo EndModifyMethodInfo = typeof(Allocator).GetMethod(nameof(Allocator.EndModify), BindingFlags.Instance | BindingFlags.NonPublic);

        private static LambdaExpression InvokeExpression<T>(Func<ValueConverter<object>, Action<Allocator, object>, Expression<Action<Allocator, T>>> func, Type type, params object[] parameters)
        {
            var delegateMethodInfo = func.Method;
            var methodInfo = delegateMethodInfo.GetGenericMethodDefinition();
            var result = methodInfo.MakeGenericMethod(type).Invoke(func.Target, parameters);
            return (LambdaExpression)result;
        }

        #region value to bytes
        private static LambdaExpression ValueToBytesExpression(Type type, ValueConverter valueConverter)
        {
            Expression<Action<Allocator, T>> Lambda<T>(ValueConverter<T> converter, Action<Allocator, T> _) =>
                (allocator, value) => converter.ToBytes(allocator, value);
            return InvokeExpression(Lambda, type, valueConverter, null);
        }
        #endregion

        #region list to bytes
        private static void ListToBytes<T>(Allocator allocator, List<T> list, ValueConverter<T> converter)
        {
            if (list != null)
                for (int i = 0; i < list.Count; i++)
                    converter.ToBytes(allocator, list[i]);
        }

        private static void ListToBytesExtend<T>(Allocator allocator, List<T> list, ValueConverter<T> converter)
        {
            if (list != null)
                for (int i = 0; i < list.Count; i++)
                    converter.ToBytesExtend(allocator, list[i]);
        }

        private static void ListToBytesExtend<T>(Allocator allocator, List<T> list, Action<Allocator, T> action)
        {
            if (list == null)
                return;
            for (int i = 0; i < list.Count; i++)
            {
                var source = allocator.BeginModify();
                action.Invoke(allocator, list[i]);
                allocator.EndModify(source);
            }
        }

        private static LambdaExpression ListToBytesLambdaExpression(Type type, ValueConverter valueConverter, Delegate @delegate)
        {
            Expression<Action<Allocator, List<T>>> Lambda<T>(ValueConverter<T> converter, Action<Allocator, T> action) =>
                action == null
                    ? converter.Length > 0
                        ? ((allocator, list) => ListToBytes(allocator, list, converter))
                        : (Expression<Action<Allocator, List<T>>>)((allocator, list) => ListToBytesExtend(allocator, list, converter))
                    : (allocator, list) => ListToBytesExtend(allocator, list, action);
            return InvokeExpression(Lambda, type, valueConverter, @delegate);
        }
        #endregion

        #region array to bytes
        private static void ArrayToBytes<T>(Allocator allocator, T[] array, ValueConverter<T> converter)
        {
            if (array != null)
                for (int i = 0; i < array.Length; i++)
                    converter.ToBytes(allocator, array[i]);
        }

        private static void ArrayToBytesExtend<T>(Allocator allocator, T[] array, ValueConverter<T> converter)
        {
            if (array != null)
                for (int i = 0; i < array.Length; i++)
                    converter.ToBytesExtend(allocator, array[i]);
        }

        private static void ArrayToBytesExtend<T>(Allocator allocator, T[] array, Action<Allocator, T> action)
        {
            if (array == null)
                return;
            for (int i = 0; i < array.Length; i++)
            {
                var source = allocator.BeginModify();
                action.Invoke(allocator, array[i]);
                allocator.EndModify(source);
            }
        }

        private static LambdaExpression ArrayToBytesLambdaExpression(Type type, ValueConverter valueConverter, Delegate @delegate)
        {
            Expression<Action<Allocator, T[]>> Lambda<T>(ValueConverter<T> converter, Action<Allocator, T> action) =>
                action == null
                    ? converter.Length > 0
                        ? ((allocator, array) => ArrayToBytes(allocator, array, converter))
                        : (Expression<Action<Allocator, T[]>>)((allocator, array) => ArrayToBytesExtend(allocator, array, converter))
                    : (allocator, array) => ArrayToBytesExtend(allocator, array, action);
            return InvokeExpression(Lambda, type, valueConverter, @delegate);
        }
        #endregion

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
                stringCache.TryAdd(key, (bytes = Encoding.UTF8.GetBytes(key)));
            return bytes;
        }

        private Delegate CreateDelegate(Type type)
        {
            if (valueConverters.TryGetValue(type, out var valueConverter))
                return ValueToBytesExpression(type, valueConverter).Compile();
            if (type.IsArray)
            {
                if (type.GetArrayRank() != 1)
                    throw new NotSupportedException("Multidimensional arrays are not supported, use array of arrays instead.");
                var elementType = type.GetElementType();
                if (elementType == typeof(object))
                    goto fail;
                var @delegate = valueConverters.TryGetValue(elementType, out var converter)
                    ? null
                    : CreateDelegate(elementType);
                var expression = ArrayToBytesLambdaExpression(elementType, converter, @delegate);
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
                    : CreateDelegate(elementType);
                var expression = ListToBytesLambdaExpression(elementType, converter, @delegate);
                return expression.Compile();
            }
            return CreateDelegateFromProperties(type);

            fail:
            throw new PacketException($"Invalid collection type: {type}");
        }

        private Delegate CreateDelegateFromProperties(Type type)
        {
            var properties = type.GetProperties();
            var instance = Expression.Parameter(type, "instance");
            var allocator = Expression.Parameter(typeof(Allocator), "allocator");
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
                    if (position == null)
                        variableList.Add(position = Expression.Variable(typeof(int), "position"));
                    list.Add(Expression.Assign(position, Expression.Call(allocator, BeginModifyMethodInfo)));
                    list.Add(Expression.Call(Expression.Constant(@delegate, delegateType), delegateType.GetMethod("Invoke"), allocator, propertyValue));
                    list.Add(Expression.Call(allocator, EndModifyMethodInfo, position));
                }
            }
            var block = Expression.Block(variableList, list);
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
