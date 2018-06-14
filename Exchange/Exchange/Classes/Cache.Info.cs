using Mikodev.Network.Converters;
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
            return Infos.TryGetValue(type, out var info)
                ? info
                : Infos.GetOrAdd(type, GetInfoFromType(type));
        }

        private static Info GetInfoFromType(Type type)
        {
            var info = new Info() { Type = type };
            if (IsBasicTypeOrEnum(info, type))
                return info;

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
                if (Convert.ToFSharpListFunc(type, elementType, out var collectionFunc, out var collectionExtendFunc))
                {
                    info.To = InfoFlags.Collection;
                    info.ToCollection = collectionFunc;
                    info.ToCollectionExtend = collectionExtendFunc;
                }
                else if (genericDefinition == typeof(List<>))
                {
                    GetInfoFromList(info, elementType, type);
                }
                else if (interfaces.Contains(typeof(IEnumerable)))
                {
                    if (Convert.ToCollectionByConstructorFunc(type, elementType, out collectionFunc, out collectionExtendFunc) ||
                        Convert.ToCollectionByAddFunc(type, elementType, out collectionFunc, out collectionExtendFunc))
                    {
                        info.ElementType = elementType;
                        info.To = InfoFlags.Collection;
                        info.ToCollection = collectionFunc;
                        info.ToCollectionExtend = collectionExtendFunc;
                    }
                }
            }
            else if (generic && genericArgs.Length == 2)
            {
                if (Convert.ToFSharpMapFunc(type, genericArgs, out var dictionaryFunc, out var dictionaryExtendFunc))
                {
                    info.To = InfoFlags.Dictionary;
                    info.ToDictionary = dictionaryFunc;
                    info.ToDictionaryExtend = dictionaryExtendFunc;
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
            info.ToDictionaryExtend = baseInfo.ToDictionaryExtend;
        }

        private static Info GetInfoFromIDictionary(Info info, Type[] genericArgs)
        {
            var elementType = typeof(KeyValuePair<,>).MakeGenericType(genericArgs);
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
            var enumerableInfo = GetInfo(enumerableType);
            GetInfoFromDictionary(info, enumerableInfo, genericArgs);
            info.To = InfoFlags.Dictionary;
            info.ToDictionary = Convert.ToDictionaryFunc(genericArgs);
            info.ToDictionaryExtend = Convert.ToDictionaryExtendFunc(genericArgs);
            return info;
        }

        private static Info GetInfoFromIList(Info info, Type[] genericArgs)
        {
            var elementType = genericArgs[0];
            info.ElementType = elementType;
            info.To = InfoFlags.Collection; // to list
            info.ToCollection = Convert.ToListFunc(elementType);
            info.ToCollectionExtend = Convert.ToListExtendFunc(elementType);
            return info;
        }

        private static Info GetInfoFromICollection(Info info, Type[] genericArgs)
        {
            var elementType = genericArgs[0];
            info.ElementType = elementType;
            info.To = InfoFlags.Collection; // to icollection
            var basicArray = GetInfo(elementType.MakeArrayType());
            info.ToCollection = basicArray.ToCollection;
            info.ToCollectionExtend = basicArray.ToCollectionExtend;
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
            info.ToCollectionExtend = basicInfo.ToCollectionExtend;
        }

        private static Type[] GetInfoFromArray(Info info, Type type)
        {
            if (type.GetArrayRank() != 1)
                throw new NotSupportedException("Multidimensional arrays are not supported, use array of arrays instead.");
            var elementType = type.GetElementType();
            if (elementType.IsEnum)
            {
                info.Converter = (PacketConverter)Activator.CreateInstance(typeof(UnmanagedArrayConverter<>).MakeGenericType(elementType));
            }
            else
            {
                info.ElementType = elementType;
                info.From = InfoFlags.Enumerable;
                info.To = InfoFlags.Collection; // to array
                info.FromEnumerable = Convert.FromArrayFunc(type, elementType);
                info.ToCollection = Convert.ToArrayFunc(elementType);
                info.ToCollectionExtend = Convert.ToArrayExtendFunc(elementType);
            }
            return new[] { elementType };
        }

        private static void GetInfoFromDictionary(Info info, Info enumerableInfo, params Type[] types)
        {
            info.From = types[0] == typeof(string) && types[1] == typeof(object)
                ? InfoFlags.Expando
                : InfoFlags.Dictionary;
            info.IndexType = types[0];
            info.ElementType = types[1];
            info.FromDictionary = enumerableInfo.FromDictionary;
            info.FromDictionaryAdapter = enumerableInfo.FromDictionaryAdapter;
        }

        private static bool IsBasicTypeOrEnum(Info info, Type type)
        {
            if (type == typeof(PacketWriter))
                info.From = InfoFlags.Writer;
            else if (type == typeof(PacketRawWriter))
                info.From = InfoFlags.RawWriter;
            else if (type == typeof(PacketReader) || type == typeof(object))
                info.To = InfoFlags.Reader;
            else if (type == typeof(PacketRawReader))
                info.To = InfoFlags.RawReader;
            else if (type.IsEnum)
                info.Converter = (PacketConverter)Activator.CreateInstance(typeof(UnmanagedValueConverter<>).MakeGenericType(type));
            else
                return false;
            return true;
        }
    }
}
