using Mikodev.Binary.Common;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace Mikodev.Binary
{
    internal static class Convert
    {
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

        private static void ListToBytesByAction<T>(Allocator allocator, List<T> list, Action<Allocator, T> action)
        {
            if (list == null)
                return;
            for (int i = 0; i < list.Count; i++)
            {
                var source = allocator.stream.BeginModify();
                action.Invoke(allocator, list[i]);
                allocator.stream.EndModify(source);
            }
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

        private static void ArrayToBytesByAction<T>(Allocator allocator, T[] array, Action<Allocator, T> action)
        {
            if (array == null)
                return;
            for (int i = 0; i < array.Length; i++)
            {
                var source = allocator.stream.BeginModify();
                action.Invoke(allocator, array[i]);
                allocator.stream.EndModify(source);
            }
        }
        #endregion


        #region expression
        private static MethodInfo GetMethodMakeGeneric(string methodName, params Type[] types)
        {
            return typeof(Convert).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic).MakeGenericMethod(types);
        }

        internal static LambdaExpression ValueToBytesExpression(Type type, ValueConverter converter)
        {
            var allocator = Expression.Parameter(typeof(Allocator), "allocator");
            var value = Expression.Parameter(type, "value");
            var expression = default(LambdaExpression);
            var methodInfo = typeof(ValueConverter<>).MakeGenericType(type).GetMethod("ToBytes");
            expression = Expression.Lambda(
                Expression.Call(
                    Expression.Constant(converter), methodInfo, allocator, value),
                allocator, value);
            return expression;
        }

        internal static LambdaExpression ListToBytesLambdaExpression(Type elementType, ValueConverter converter, Delegate @delegate)
        {
            return ToBytesLambdaExpression(elementType, typeof(List<>).MakeGenericType(elementType), converter, @delegate, nameof(ListToBytes), nameof(ListToBytesExtend), nameof(ListToBytesByAction));
        }

        internal static LambdaExpression ArrayToBytesLambdaExpression(Type elementType, ValueConverter converter, Delegate @delegate)
        {
            return ToBytesLambdaExpression(elementType, elementType.MakeArrayType(), converter, @delegate, nameof(ArrayToBytes), nameof(ArrayToBytesExtend), nameof(ArrayToBytesByAction));
        }

        private static LambdaExpression ToBytesLambdaExpression(Type elementType, Type collectionType, ValueConverter converter, Delegate @delegate, string method, string extendMetod, string byActionMethod)
        {
            var allocator = Expression.Parameter(typeof(Allocator), "allocator");
            var collection = Expression.Parameter(collectionType, "collection");
            var expression = default(LambdaExpression);
            if (@delegate != null)
            {
                var methodInfo = GetMethodMakeGeneric(byActionMethod, elementType);
                expression = Expression.Lambda(
                    Expression.Call(
                        methodInfo, allocator, collection, Expression.Constant(@delegate)),
                    allocator, collection);
            }
            else
            {
                var methodInfo = GetMethodMakeGeneric(
                    converter.Length > 0 ? method : extendMetod,
                    elementType);
                expression = Expression.Lambda(
                    Expression.Call(
                        methodInfo, allocator, collection, Expression.Constant(converter)),
                    allocator, collection);
            }
            return expression;
        }
        #endregion

    }
}
