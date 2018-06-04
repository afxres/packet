using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using FromDictionaryAdapterFunction = System.Func<Mikodev.Network.PacketConverter, object, System.Collections.Generic.IEnumerable<System.Collections.Generic.KeyValuePair<byte[], object>>>;
using FromDictionaryFunction = System.Func<Mikodev.Network.PacketConverter, Mikodev.Network.PacketConverter, object, System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<byte[], byte[]>>>;
using FromEnumerableFunction = System.Func<Mikodev.Network.PacketConverter, object, byte[][]>;
using ToCollectionExtendFunction = System.Func<object[], object>;
using ToCollectionFunction = System.Func<Mikodev.Network.PacketReader, Mikodev.Network.PacketConverter, object>;
using ToCollectionSpecialFunction = System.Func<Mikodev.Network.PacketReader, Mikodev.Network.PacketConverter, object>;
using ToDictionaryExtendFunction = System.Func<System.Collections.Generic.List<object>, object>;
using ToDictionaryFunction = System.Func<Mikodev.Network.PacketReader, Mikodev.Network.PacketConverter, Mikodev.Network.PacketConverter, object>;
using ToEnumerableAdapterFunction = System.Func<Mikodev.Network.PacketReader, Mikodev.Network.Info, int, object>;

namespace Mikodev.Network
{
    partial class Convert
    {
        private static readonly MethodInfo ConvertArrayMethodInfo = typeof(Array).GetMethod(nameof(Array.Copy), new[] { typeof(Array), typeof(Array), typeof(int) });

        private static readonly MethodInfo ToArrayMethodInfo = typeof(Convert).GetMethod(nameof(ToArray), BindingFlags.Static | BindingFlags.NonPublic);

        private static Expression ConvertArrayExpression(Type elementType, out ParameterExpression objectArray)
        {
            objectArray = Expression.Parameter(typeof(object[]), "objectArray");
            if (elementType == typeof(object))
                return objectArray;
            var arrayLength = Expression.ArrayLength(objectArray);
            var targetArray = Expression.Variable(elementType.MakeArrayType(), "targetArray");
            var block = Expression.Block(
                new[] { targetArray },
                Expression.Assign(targetArray, Expression.NewArrayBounds(elementType, arrayLength)),
                Expression.Call(ConvertArrayMethodInfo, objectArray, targetArray, arrayLength),
                targetArray);
            return block;
        }

        private static ToCollectionFunction InternalToCollectionFunc(string methodName, Type elementType)
        {
            var reader = Expression.Parameter(typeof(PacketReader), "reader");
            var converter = Expression.Parameter(typeof(PacketConverter), "converter");
            var methodInfo = typeof(Convert).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            var expression = Expression.Lambda<ToCollectionFunction>(
                Expression.Convert(
                    Expression.Call(
                        methodInfo.MakeGenericMethod(elementType),
                        reader,
                        converter),
                    typeof(object)),
                reader, converter);
            return expression.Compile();
        }

        private static ToCollectionExtendFunction InternalToCollectionExtendFunc(Type elementType, Func<Expression, Expression> expressionFunc)
        {
            var block = ConvertArrayExpression(elementType, out var objectArray);
            var conversion = expressionFunc.Invoke(block);
            if (conversion.Type != typeof(object))
                conversion = Expression.Convert(conversion, typeof(object));
            var expression = Expression.Lambda<ToCollectionExtendFunction>(conversion, objectArray);
            return expression.Compile();
        }

        private static TDelegate InternalCreateDelegate<TDelegate>(string methodName, params Type[] types) where TDelegate : Delegate
        {
            var methodInfo = typeof(Convert).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            var lambdaExpression = (LambdaExpression)methodInfo.MakeGenericMethod(types).Invoke(null, null);
            return (TDelegate)lambdaExpression.Compile();
        }

        private static ConstructorInfo InternalListConstructorInfo(Type elementType) => typeof(List<>).MakeGenericType(elementType).GetConstructor(new[] { typeof(IEnumerable<>).MakeGenericType(elementType) });

        private static FromEnumerableFunction InternalFromEnumerableFunc(string methodName, Type enumerableType, Type elementType)
        {
            var value = Expression.Parameter(typeof(object), "value");
            var converter = Expression.Parameter(typeof(PacketConverter), "converter");
            var methodInfo = typeof(Convert).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            var expression = Expression.Lambda<FromEnumerableFunction>(
                Expression.Call(
                    methodInfo.MakeGenericMethod(elementType),
                    converter,
                    Expression.Convert(value, enumerableType)),
                converter, value);
            return expression.Compile();
        }

        private static Expression<ToEnumerableAdapterFunction> ToEnumerableAdapterExpression<T>() => (reader, info, level) => new EnumerableAdapter<T>(reader, info, level);

        private static Expression ToCollectionByAddExpression(Type elementType, Expression value, ConstructorInfo constructorInfo, MethodInfo addMethodInfo)
        {
            var instance = Expression.Variable(constructorInfo.DeclaringType, "collection");
            var array = Expression.Variable(value.Type, "array");
            var index = Expression.Variable(typeof(int), "index");
            var label = Expression.Label(typeof(object), "result");
            var arrayAccess = Expression.ArrayAccess(array, Expression.PostIncrementAssign(index)) as Expression;
            if (arrayAccess.Type != elementType)
                arrayAccess = Expression.Convert(arrayAccess, elementType);
            var block = Expression.Block(
                new[] { instance, array, index },
                Expression.Assign(instance, Expression.New(constructorInfo)),
                Expression.Assign(array, value),
                Expression.Assign(index, Expression.Constant(0)),
                Expression.Loop(
                    Expression.IfThenElse(
                        Expression.LessThan(index, Expression.ArrayLength(array)),
                        Expression.Call(instance, addMethodInfo, arrayAccess),
                        Expression.Break(label, Expression.Convert(instance, typeof(object)))),
                    label));
            return block;
        }

        #region array, list, sequence
        internal static ToCollectionFunction ToArrayFunc(Type elementType) => InternalToCollectionFunc(nameof(ToArray), elementType);

        internal static ToCollectionFunction ToListFunc(Type elementType) => InternalToCollectionFunc(nameof(ToList), elementType);

        internal static ToCollectionFunction ToEnumerableFunc(Type elementType) => InternalToCollectionFunc(nameof(ToEnumerable), elementType);

        internal static ToCollectionExtendFunction ToArrayExtendFunc(Type elementType) => InternalToCollectionExtendFunc(elementType, block => block);

        internal static ToCollectionExtendFunction ToListExtendFunc(Type elementType) => InternalToCollectionExtendFunc(elementType, block => Expression.New(InternalListConstructorInfo(elementType), block));

        internal static ToEnumerableAdapterFunction ToEnumerableAdapterFunc(Type elementType) => InternalCreateDelegate<ToEnumerableAdapterFunction>(nameof(ToEnumerableAdapterExpression), elementType);

        internal static FromEnumerableFunction FromArrayFunc(Type type, Type elementType) => InternalFromEnumerableFunc(nameof(FromArray), type, elementType);

        internal static FromEnumerableFunction FromListFunc(Type type, Type elementType) => InternalFromEnumerableFunc(nameof(FromList), type, elementType);

        internal static FromEnumerableFunction FromEnumerableFunc(Type type, Type elementType) => InternalFromEnumerableFunc(nameof(FromEnumerable), type, elementType);
        #endregion

        #region dictionary
        private static Expression<FromDictionaryFunction> FromDictionaryExpression<TK, TV>() => (index, element, dictionary) => FromDictionary(index, element, (IEnumerable<KeyValuePair<TK, TV>>)dictionary);

        private static Expression<FromDictionaryAdapterFunction> FromDictionaryAdapterExpression<TK, TV>() => (converter, dictionary) => new DictionaryAdapter<TK, TV>(converter, (IEnumerable<KeyValuePair<TK, TV>>)dictionary);

        private static Expression<ToDictionaryExtendFunction> ToDictionaryExtendExpression<TK, TV>() => list => ToDictionaryExtend<TK, TV>(list);

        private static Expression<ToDictionaryFunction> ToDictionaryExpression<TK, TV>() => (reader, index, element) => ToDictionary<TK, TV>(reader, index, element);

        internal static ToDictionaryFunction ToDictionaryFunc(params Type[] types) => InternalCreateDelegate<ToDictionaryFunction>(nameof(ToDictionaryExpression), types);

        internal static ToDictionaryExtendFunction ToDictionaryExtendFunc(params Type[] types) => InternalCreateDelegate<ToDictionaryExtendFunction>(nameof(ToDictionaryExtendExpression), types);

        internal static FromDictionaryFunction FromDictionaryFunc(params Type[] types) => InternalCreateDelegate<FromDictionaryFunction>(nameof(FromDictionaryExpression), types);

        internal static FromDictionaryAdapterFunction FromDictionaryAdapterFunc(params Type[] types) => InternalCreateDelegate<FromDictionaryAdapterFunction>(nameof(FromDictionaryAdapterExpression), types);
        #endregion

        #region to collection
        internal static bool ToCollectionByConstructorFunc(Type type, Type elementType, out ToCollectionSpecialFunction collectionFunc, out ToCollectionExtendFunction collectionExtendFunc)
        {
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
            var constructorInfo = type.GetConstructor(new[] { enumerableType });
            if (constructorInfo == null)
            {
                collectionFunc = null;
                collectionExtendFunc = null;
                return false;
            }

            var reader = Expression.Parameter(typeof(PacketReader), "reader");
            var converter = Expression.Parameter(typeof(PacketConverter), "converter");
            var expression = Expression.Lambda<ToCollectionSpecialFunction>(
                Expression.Convert(
                    Expression.New(
                        constructorInfo,
                        Expression.Convert(
                            Expression.Call(ToArrayMethodInfo.MakeGenericMethod(elementType), reader, converter),
                            enumerableType)),
                    typeof(object)),
                reader, converter);
            collectionFunc = expression.Compile();
            collectionExtendFunc = InternalToCollectionExtendFunc(elementType, block => Expression.New(constructorInfo, block));
            return true;
        }

        internal static bool ToCollectionByAddFunc(Type type, Type elementType, out ToCollectionSpecialFunction collectionFunc, out ToCollectionExtendFunction collectionExtendFunc)
        {
            var constructorInfo = type.GetConstructor(Type.EmptyTypes);
            var addMethodInfo = type.GetMethod("Add", BindingFlags.Instance | BindingFlags.Public, null, new[] { elementType }, null);
            if (constructorInfo == null || addMethodInfo == null)
            {
                collectionFunc = null;
                collectionExtendFunc = null;
                return false;
            }

            var reader = Expression.Parameter(typeof(PacketReader), "reader");
            var converter = Expression.Parameter(typeof(PacketConverter), "converter");
            var expression = Expression.Lambda<Func<PacketReader, PacketConverter, object>>(
                ToCollectionByAddExpression(
                    elementType,
                    Expression.Call(ToArrayMethodInfo.MakeGenericMethod(elementType), reader, converter),
                    constructorInfo, addMethodInfo),
                reader, converter);
            collectionFunc = expression.Compile();

            var objectArray = Expression.Parameter(typeof(object[]), "objectArray");
            var extensionExpression = Expression.Lambda<ToCollectionExtendFunction>(
                ToCollectionByAddExpression(
                    elementType,
                    objectArray,
                    constructorInfo, addMethodInfo),
                objectArray);
            collectionExtendFunc = extensionExpression.Compile();
            return true;
        }
        #endregion
    }
}
