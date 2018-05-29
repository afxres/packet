using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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
                info.Flag = InfoFlags.Enum;
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
                var elementType = genericArgs[0];
                if (Convert.ToFSharpListFunc(type, elementType, out var collectionFunc, out var collectionExtFunc))
                {
                    info.To = InfoFlags.Collection;
                    info.ToCollection = collectionFunc;
                    info.ToCollectionExt = collectionExtFunc;
                }
                else if (genericDefinition == typeof(List<>))
                {
                    GetInfoFromList(info, elementType, type);
                }
                else if (interfaces.Contains(typeof(IEnumerable)))
                {
                    if (Convert.ToCollectionByConstructorFunc(type, elementType, out collectionFunc, out collectionExtFunc) ||
                        Convert.ToCollectionByAddFunc(type, elementType, out collectionFunc, out collectionExtFunc))
                    {
                        info.ElementType = elementType;
                        info.To = InfoFlags.Collection;
                        info.ToCollection = collectionFunc;
                        info.ToCollectionExt = collectionExtFunc;
                    }
                }
            }
            else if (generic && genericArgs.Length == 2)
            {
                if (Convert.ToFSharpMapFunc(type, genericArgs, out var dictionaryFunc, out var dictionaryExtFunc))
                {
                    info.To = InfoFlags.Dictionary;
                    info.ToDictionary = dictionaryFunc;
                    info.ToDictionaryExt = dictionaryExtFunc;
                }
                else if (genericDefinition == typeof(Dictionary<,>))
                {
                    GetInfoFromDictionary(info, genericArgs);
                }
            }

            if (info.From == InfoFlags.None)
            {
                GetInfoFindImplementation(info, genericArgs, interfaces);
            }

            if (info.From == InfoFlags.Enumerable)
            {
                if (info.ElementType == typeof(byte) && interfaces.Contains(typeof(ICollection<byte>)))
                    info.From = InfoFlags.Bytes;
                else if (info.ElementType == typeof(sbyte) && interfaces.Contains(typeof(ICollection<sbyte>)))
                    info.From = InfoFlags.SBytes;
            }
            return info;
        }

        private static void GetInfoFindImplementation(Info info, Type[] genericArgs, Type[] interfaces)
        {
            var list = interfaces
                .Where(r => r.IsGenericType && r.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .Select(GetInfo)
                .ToList();
            if (list.Count > 1 && genericArgs != null)
            {
                var comparer = default(Type);
                if (genericArgs.Length == 1)
                    comparer = genericArgs[0];
                else if (genericArgs.Length == 2)
                    comparer = typeof(KeyValuePair<,>).MakeGenericType(genericArgs);
                if (comparer != null)
                    list = list.Where(r => r.ElementType == comparer).ToList();
            }

            if (list.Count == 1)
            {
                var enumerable = list[0];
                var elementType = enumerable.ElementType;
                if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    GetInfoFromDictionary(info, list[0], elementType.GetGenericArguments());
                }
                else
                {
                    info.From = InfoFlags.Enumerable;
                    info.ElementType = enumerable.ElementType;
                    info.FromEnumerable = enumerable.FromEnumerable;
                }
            }
        }

        private static void GetInfoFromDictionary(Info info, Type[] genericArgs)
        {
            var baseType = typeof(IDictionary<,>).MakeGenericType(genericArgs);
            var baseInfo = GetInfo(baseType);
            info.To = InfoFlags.Dictionary;
            info.ToDictionary = baseInfo.ToDictionary;
            info.ToDictionaryExt = baseInfo.ToDictionaryExt;
        }

        private static Info GetInfoFromIDictionary(Info info, Type[] genericArgs)
        {
            var elementType = typeof(KeyValuePair<,>).MakeGenericType(genericArgs);
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
            var enumerableInfo = GetInfo(enumerableType);
            GetInfoFromDictionary(info, enumerableInfo, genericArgs);
            info.To = InfoFlags.Dictionary;
            info.ToDictionary = Convert.ToDictionaryFunc(genericArgs);
            info.ToDictionaryExt = Convert.ToDictionaryExtFunc(genericArgs);
            return info;
        }

        private static Info GetInfoFromIList(Info info, Type[] genericArgs)
        {
            var elementType = genericArgs[0];
            info.ElementType = elementType;
            info.To = InfoFlags.Collection; // to list
            info.ToCollection = Convert.ToListFunc(elementType);
            info.ToCollectionExt = Convert.ToListExtFunc(elementType);
            return info;
        }

        private static Info GetInfoFromICollection(Info info, Type[] genericArgs)
        {
            var elementType = genericArgs[0];
            info.ElementType = elementType;
            info.To = InfoFlags.Collection; // to icollection
            var basicArray = GetInfo(elementType.MakeArrayType());
            info.ToCollection = basicArray.ToCollection;
            info.ToCollectionExt = basicArray.ToCollectionExt;
            return info;
        }

        private static Info GetInfoFromIEnumerable(Info info, Type[] genericArgs, Type interfaceType)
        {
            var elementType = genericArgs[0];
            info.ElementType = elementType;
            info.To = InfoFlags.Enumerable;
            info.ToEnumerable = Convert.ToEnumerableFunc(elementType);
            info.ToEnumerableAdapter = Convert.ToEnumerableAdapterFunc(elementType);

            // From ... function
            info.From = InfoFlags.Enumerable;
            info.FromEnumerable = Convert.FromEnumerableFunc(interfaceType, elementType);
            if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
            {
                var arguments = elementType.GetGenericArguments();
                info.FromDictionary = Convert.FromDictionaryFunc(arguments);
                info.FromDictionaryAdapter = Convert.FromDictionaryAdapterFunc(arguments);
            }
            return info;
        }

        private static void GetInfoFromList(Info info, Type elementType, Type type)
        {
            var basicInfo = GetInfo(typeof(IList<>).MakeGenericType(elementType));
            info.ElementType = elementType;
            info.From = InfoFlags.Enumerable;
            info.To = InfoFlags.Collection; // to list
            info.FromEnumerable = Convert.FromListFunc(type, elementType);
            info.ToCollection = basicInfo.ToCollection;
            info.ToCollectionExt = basicInfo.ToCollectionExt;
        }

        private static Type[] GetInfoFromArray(Info info, Type type)
        {
            if (type.GetArrayRank() != 1)
                throw new NotSupportedException("Multidimensional arrays are not supported, use array of arrays instead.");
            var elementType = type.GetElementType();
            info.ElementType = elementType;

            info.From = InfoFlags.Enumerable;
            info.To = InfoFlags.Collection; // to array
            info.FromEnumerable = Convert.FromArrayFunc(type, elementType);
            info.ToCollection = Convert.ToArrayFunc(elementType);
            info.ToCollectionExt = Convert.ToArrayExtFunc(elementType);
            return new[] { elementType };
        }

        private static void GetInfoFromDictionary(Info info, Info enumerableInfo, params Type[] types)
        {
            if (types[0] == typeof(string) && types[1] == typeof(object))
                info.From = InfoFlags.Expando;
            else
                info.From = InfoFlags.Dictionary;
            info.IndexType = types[0];
            info.ElementType = types[1];
            info.FromDictionary = enumerableInfo.FromDictionary;
            info.FromDictionaryAdapter = enumerableInfo.FromDictionaryAdapter;
        }

        private static bool IsBasicType(Info info, Type type)
        {
            if (type == typeof(PacketWriter))
                info.From = InfoFlags.Writer;
            else if (type == typeof(PacketRawWriter))
                info.From = InfoFlags.RawWriter;
            else if (type == typeof(PacketReader) || type == typeof(object))
                info.To = InfoFlags.Reader;
            else if (type == typeof(PacketRawReader))
                info.To = InfoFlags.RawReader;
            else
                return false;
            return true;
        }
    }
}
