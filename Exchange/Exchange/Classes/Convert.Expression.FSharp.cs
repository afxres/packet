using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ToCollectionExtFunction = System.Func<object[], object>;
using ToCollectionFunction = System.Func<Mikodev.Network.PacketReader, Mikodev.Network.PacketConverter, object>;
using ToDictionaryExtFunction = System.Func<System.Collections.Generic.List<object>, object>;
using ToDictionaryFunction = System.Func<Mikodev.Network.PacketReader, Mikodev.Network.PacketConverter, Mikodev.Network.PacketConverter, object>;

namespace Mikodev.Network
{
    partial class Convert
    {
        private const string FSharpCollectionsNamespace = "Microsoft.FSharp.Collections";

        private static MethodInfo ToFSharpListMethodInfo;

        private static bool InternalIsFSharpList(Type type)
        {
            if (type.Name != "FSharpList`1" || type.Namespace != FSharpCollectionsNamespace)
                return false;
            var methodInfo = ToFSharpListMethodInfo;
            if (methodInfo == null)
            {
                try
                {
                    methodInfo = type.Assembly.GetType("Microsoft.FSharp.Collections.ArrayModule", false, false)
                        ?.GetMethods()
                        .Where(r => r.Name == "ToList")
                        .Single();
                }
                catch (Exception ex)
                {
                    throw new Exception("No valid F# toList method detected", ex);
                }
            }
            ToFSharpListMethodInfo = methodInfo;
            return true;
        }

        private static bool InternalIsFSharpMap(Type type, Type[] elementTypes, out ConstructorInfo constructorInfo)
        {
            if (type.Name != "FSharpMap`2" || type.Namespace != FSharpCollectionsNamespace)
                goto fail;
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(typeof(Tuple<,>).MakeGenericType(elementTypes));
            constructorInfo = type.GetConstructor(new[] { enumerableType });
            return constructorInfo != null;

            fail:
            constructorInfo = null;
            return false;
        }

        internal static bool ToFSharpListFunc(Type type, Type elementType, out ToCollectionFunction collectionFunc, out ToCollectionExtFunction collectionExtFunc)
        {
            if (InternalIsFSharpList(type) == false)
            {
                collectionFunc = null;
                collectionExtFunc = null;
                return false;
            }

            var reader = Expression.Parameter(typeof(PacketReader), "reader");
            var converter = Expression.Parameter(typeof(PacketConverter), "converter");
            var expression = Expression.Lambda<ToCollectionFunction>(
                Expression.Convert(
                    Expression.Call(
                        ToFSharpListMethodInfo.MakeGenericMethod(elementType),
                        Expression.Call(
                            ToArrayMethodInfo.MakeGenericMethod(elementType),
                            reader, converter)),
                    typeof(object)),
                reader, converter);
            collectionFunc = expression.Compile();

            var extensionExpression = Expression.Lambda<ToCollectionExtFunction>(
                Expression.Convert(
                    Expression.Call(
                        ToFSharpListMethodInfo.MakeGenericMethod(elementType),
                        ConvertArrayExpression(elementType, out var objectArray)),
                    typeof(object)),
                objectArray);
            collectionExtFunc = extensionExpression.Compile();
            return true;
        }

        internal static bool ToFSharpMapFunc(Type type, Type[] elementTypes, out ToDictionaryFunction dictionaryFunc, out ToDictionaryExtFunction dictionaryExtFunc)
        {
            if (InternalIsFSharpMap(type, elementTypes, out var constructorInfo) == false)
            {
                dictionaryFunc = null;
                dictionaryExtFunc = null;
                return false;
            }

            var tupleListMothodInfo = typeof(Convert).GetMethod(nameof(ToTupleList), BindingFlags.Static | BindingFlags.NonPublic);
            var tupleListExtMothodInfo = typeof(Convert).GetMethod(nameof(ToTupleListExt), BindingFlags.Static | BindingFlags.NonPublic);
            var reader = Expression.Parameter(typeof(PacketReader), "reader");
            var indexConverter = Expression.Parameter(typeof(PacketConverter), "index");
            var elementConverter = Expression.Parameter(typeof(PacketConverter), "element");
            var expression = Expression.Lambda<ToDictionaryFunction>(
                Expression.Convert(
                    Expression.New(
                        constructorInfo,
                        Expression.Call(
                            tupleListMothodInfo.MakeGenericMethod(elementTypes),
                            reader, indexConverter, elementConverter)),
                    typeof(object)),
                reader, indexConverter, elementConverter);
            dictionaryFunc = expression.Compile();

            var objectList = Expression.Parameter(typeof(List<object>), "objectList");
            var extensionExpression = Expression.Lambda<ToDictionaryExtFunction>(
                Expression.Convert(
                    Expression.New(
                        constructorInfo,
                        Expression.Call(
                            tupleListExtMothodInfo.MakeGenericMethod(elementTypes),
                            objectList)),
                    typeof(object)),
                objectList);
            dictionaryExtFunc = extensionExpression.Compile();
            return true;
        }
    }
}
