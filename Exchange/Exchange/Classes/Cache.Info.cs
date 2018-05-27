using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Mikodev.Network
{
    partial class Cache
    {
        internal static Info GetInfo(Type type)
        {
            if (Infos.TryGetValue(type, out var inf))
                return inf;
            return Infos.GetOrAdd(type, GetInfoFromType(type));
        }

        private static Info GetInfoFromType(Type type)
        {
            var info = new Info() { Type = type };
            if (IsBasicType(info, type))
                return info;
            if (type.IsEnum)
            {
                info.Flag = Info.Enum;
                info.ElementType = Enum.GetUnderlyingType(type);
                return info;
            }

            var generic = type.IsGenericType;
            var genericArgs = generic ? type.GetGenericArguments() : null;
            var genericDefinition = generic ? type.GetGenericTypeDefinition() : null;

            if (generic && type.IsInterface)
            {
                if (genericDefinition == typeof(IEnumerable<>))
                    return GetInfoFromIEnumerable(info, genericArgs, type);
                if (genericDefinition == typeof(ICollection<>))
                    return GetInfoFromICollection(info, genericArgs);
                if (genericDefinition == typeof(IList<>))
                    return GetInfoFromIList(info, genericArgs);
                if (genericDefinition == typeof(IDictionary<,>))
                    return GetInfoFromIDictionary(info, genericArgs);
            }

            var interfaces = type.GetInterfaces();
            if (type.IsArray)
            {
                genericArgs = GetInfoFromArray(info, type);
            }
            else if (generic && genericArgs.Length == 1)
            {
                if (IsFSharpList(type))
                    GetInfoFromFSharpList(info, genericArgs[0]);
                else if (genericDefinition == typeof(List<>))
                    GetInfoFromList(info, genericArgs[0], type);
                else if (interfaces.Contains(typeof(IEnumerable)))
                    GetInfoFromCollection(info, genericArgs[0], type);
            }
            else if (generic && genericArgs.Length == 2)
            {
                if (IsFSharpMap(type, genericArgs, out var constructorInfo))
                    GetInfoFromFSharpMap(info, genericArgs, constructorInfo);
                else if (genericDefinition == typeof(Dictionary<,>))
                    GetInfoFromDictionary(info, genericArgs);
            }

            if (info.From == Info.None)
            {
                GetInfoFindImplementation(info, genericArgs, interfaces);
            }

            if (info.From == Info.Enumerable)
            {
                if (info.ElementType == typeof(byte) && interfaces.Contains(typeof(ICollection<byte>)))
                    info.From = Info.Bytes;
                else if (info.ElementType == typeof(sbyte) && interfaces.Contains(typeof(ICollection<sbyte>)))
                    info.From = Info.SBytes;
            }
            return info;
        }

        private static void GetInfoFindImplementation(Info info, Type[] genericArgs, Type[] interfaces)
        {
            if (interfaces.Contains(typeof(IDictionary<string, object>)))
                info.From = Info.Map;

            var baseList = interfaces
                .Where(r => r.IsGenericType && r.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .Select(GetInfo)
                .ToList();
            if (baseList.Count > 1 && genericArgs != null)
            {
                var comparer = default(Type);
                if (genericArgs.Length == 1)
                    comparer = genericArgs[0];
                else if (genericArgs.Length == 2)
                    comparer = typeof(KeyValuePair<,>).MakeGenericType(genericArgs);
                if (comparer != null)
                    baseList = baseList.Where(r => r.ElementType == comparer).ToList();
            }

            if (baseList.Count == 1)
            {
                var enumerable = baseList[0];
                var elementType = enumerable.ElementType;
                if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    GetInfoFromDictionary(info, baseList[0], elementType.GetGenericArguments());
                }
                else
                {
                    info.From = Info.Enumerable;
                    info.ElementType = enumerable.ElementType;
                    info.FromEnumerable = enumerable.FromEnumerable;
                }
            }
        }

        private static void GetInfoFromDictionary(Info info, Type[] genericArgs)
        {
            var baseType = typeof(IDictionary<,>).MakeGenericType(genericArgs);
            var baseInfo = GetInfo(baseType);
            info.To = Info.Dictionary;
            info.ToDictionary = baseInfo.ToDictionary;
            info.ToDictionaryCast = baseInfo.ToDictionaryCast;
        }

        private static void GetInfoFromFSharpMap(Info info, Type[] genericArgs, ConstructorInfo constructorInfo)
        {
            info.To = Info.Dictionary;
            info.ToDictionary = GetToFSharpMapFunction(constructorInfo, genericArgs);
            info.ToDictionaryCast = GetCastFSharpMapFunction(constructorInfo, genericArgs);
        }

        private static Info GetInfoFromIDictionary(Info info, Type[] genericArgs)
        {
            var elementType = typeof(KeyValuePair<,>).MakeGenericType(genericArgs);
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
            var enumerableInfo = GetInfo(enumerableType);
            GetInfoFromDictionary(info, enumerableInfo, genericArgs);
            info.To = Info.Dictionary;
            info.ToDictionary = GetToDictionaryFunction(genericArgs);
            info.ToDictionaryCast = GetCastDictionaryFunction(genericArgs);
            return info;
        }

        private static Info GetInfoFromIList(Info info, Type[] genericArgs)
        {
            var elementType = genericArgs[0];
            info.ElementType = elementType;
            info.To = Info.Collection; // to list
            info.ToCollection = GetToFunction(ToListMethodInfo, elementType);
            info.ToCollectionCast = GetCastListFunction(elementType); // cast to list
            return info;
        }

        private static Info GetInfoFromICollection(Info info, Type[] genericArgs)
        {
            var elementType = genericArgs[0];
            info.ElementType = elementType;
            info.To = Info.Collection; // to icollection
            var basicArray = GetInfo(elementType.MakeArrayType());
            info.ToCollection = basicArray.ToCollection;
            info.ToCollectionCast = basicArray.ToCollectionCast;
            return info;
        }

        private static Info GetInfoFromIEnumerable(Info info, Type[] genericArgs, Type interfaceType)
        {
            var elementType = genericArgs[0];
            info.ElementType = elementType;
            info.To = Info.Enumerable;
            info.ToEnumerable = GetToFunction(ToEnumerableMethodInfo, elementType);
            info.ToEnumerableAdapter = GetToEnumerableAdapter(elementType);

            // From ... function
            info.From = Info.Enumerable;
            info.FromEnumerable = GetFromEnumerableFunction(FromEnumerableMethodInfo.MakeGenericMethod(elementType), interfaceType);
            if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                var arguments = elementType.GetGenericArguments();
                info.FromDictionary = GetFromDictionaryFunction(arguments);
                info.FromDictionaryAdapter = GetFromAdapterFunction(arguments);
            }
            return info;
        }

        private static void GetInfoFromFSharpList(Info info, Type elementType)
        {
            info.To = Info.Collection;
            info.ToCollection = GetToFSharpListFunction(elementType);
            info.ToCollectionCast = GetCastFSharpListFunction(elementType);
        }

        private static void GetInfoFromList(Info info, Type elementType, Type type)
        {
            var basicInfo = GetInfo(typeof(IList<>).MakeGenericType(elementType));
            info.ElementType = elementType;
            info.From = Info.Enumerable;
            info.To = Info.Collection; // to list
            info.FromEnumerable = GetFromEnumerableFunction(FromListMethodInfo.MakeGenericMethod(elementType), type);
            info.ToCollection = basicInfo.ToCollection;
            info.ToCollectionCast = basicInfo.ToCollectionCast;
        }

        private static void GetInfoFromCollection(Info info, Type elementType, Type type)
        {
            var functor = default(Func<PacketReader, PacketConverter, object>);
            if ((functor = GetToCollectionFunction(type, elementType, out var constructor)) != null)
            {
                /* .ctor(IEnumerable<T> items) */
                info.ElementType = elementType;
                info.To = Info.Collection;
                info.ToCollection = functor;
                info.ToCollectionCast = GetCastCollectionFunction(elementType, constructor);
            }
            else if ((functor = GetToCollectionFunction(type, elementType, out var non, out var add)) != null)
            {
                /* foreach -> Add(T item) */
                info.ElementType = elementType;
                info.To = Info.Collection;
                info.ToCollection = functor;
                info.ToCollectionCast = GetCastCollectionFunction(elementType, non, add);
            }
        }

        private static Type[] GetInfoFromArray(Info info, Type type)
        {
            if (type.GetArrayRank() != 1)
                throw new NotSupportedException("Multidimensional arrays are not supported, use array of arrays instead.");
            var elementType = type.GetElementType();
            info.ElementType = elementType;

            info.From = Info.Enumerable;
            info.To = Info.Collection; // to array
            info.FromEnumerable = GetFromEnumerableFunction(FromArrayMethodInfo.MakeGenericMethod(elementType), type);
            info.ToCollection = GetToFunction(ToArrayMethodInfo, elementType);
            info.ToCollectionCast = GetCastArrayFunction(elementType); // cast to array
            return new[] { elementType };
        }

        private static void GetInfoFromDictionary(Info info, Info enumerableInfo, params Type[] types)
        {
            if (types[0] == typeof(string) && types[1] == typeof(object))
                info.From = Info.Map;
            else
                info.From = Info.Dictionary;
            info.IndexType = types[0];
            info.ElementType = types[1];
            info.FromDictionary = enumerableInfo.FromDictionary;
            info.FromDictionaryAdapter = enumerableInfo.FromDictionaryAdapter;
        }

        private static bool IsBasicType(Info info, Type type)
        {
            if (type == typeof(PacketWriter))
                info.From = Info.Writer;
            else if (type == typeof(PacketRawWriter))
                info.From = Info.RawWriter;
            else if (type == typeof(PacketReader) || type == typeof(object))
                info.To = Info.Reader;
            else if (type == typeof(PacketRawReader))
                info.To = Info.RawReader;
            else
                return false;
            return true;
        }
    }
}
