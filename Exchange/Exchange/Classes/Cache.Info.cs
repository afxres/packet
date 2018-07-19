using Mikodev.Network.Converters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mikodev.Network
{
    partial class Cache
    {
        internal static Info GetInfo(Type type)
        {
            return Infos.TryGetValue(type, out var info) ? info : Infos.GetOrAdd(type, GetInfoFromType(type));
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

        private static Info GetInfoFromType(Type type)
        {
            var info = new Info() { Type = type };
            if (IsBasicTypeOrEnum(info, type))
                return info;

            var generic = type.IsGenericType;
            var genericArguments = generic ? type.GetGenericArguments() : null;
            var genericDefinition = generic ? type.GetGenericTypeDefinition() : null;

            #region dictionary, map...
            if (genericDefinition == typeof(KeyValuePair<,>))
                throw PacketException.InvalidType(type);
            if (Convert.ToFSharpMapFunc(type, genericArguments, out var dictionaryFunc, out var dictionaryExtendFunc))
            {
                info.To = InfoFlags.Dictionary;
                info.ToDictionary = dictionaryFunc;
                info.ToDictionaryExtend = dictionaryExtendFunc;
            }
            else if (genericDefinition == typeof(Dictionary<,>))
            {
                info.To = InfoFlags.Dictionary;
                info.ToDictionary = Convert.ToDictionaryFunc(genericArguments);
                info.ToDictionaryExtend = Convert.ToDictionaryExtendFunc(genericArguments);
            }
            else if (genericDefinition == typeof(IDictionary<,>))
            {
                var dictionaryInfo = GetInfo(typeof(Dictionary<,>).MakeGenericType(genericArguments));
                info.To = InfoFlags.Dictionary;
                info.ToDictionary = dictionaryInfo.ToDictionary;
                info.ToDictionaryExtend = dictionaryInfo.ToDictionaryExtend;
            }

            var interfaces = type.GetInterfaces();
            var enumerableTypes = interfaces.Where(r => r.IsGenericType && r.GetGenericTypeDefinition() == typeof(IEnumerable<>)).ToArray();
            if (enumerableTypes.Length > 1)
                throw new PacketException(PacketError.InvalidType, $"Multiple IEnumerable implementations, type : {type}");
            if (enumerableTypes.Length == 1)
            {
                var argument = enumerableTypes[0].GetGenericArguments().Single();
                if (argument.IsGenericType && argument.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    if (info.To != InfoFlags.Dictionary)
                        throw PacketException.InvalidType(type);
                    var arguments = argument.GetGenericArguments();
                    info.IndexType = arguments[0];
                    info.ElementType = arguments[1];
                    info.From = (arguments[0] == typeof(string) && arguments[1] == typeof(object)) ? InfoFlags.Expando : InfoFlags.Dictionary;
                    info.FromDictionary = Convert.FromDictionaryFunc(arguments);
                    info.FromDictionaryAdapter = Convert.FromDictionaryAdapterFunc(arguments);
                    return info;
                }
            }
            #endregion

            if (genericDefinition == typeof(IEnumerable<>))
            {
                var elementType = genericArguments[0];
                info.ElementType = elementType;
                info.To = InfoFlags.Enumerable;
                info.ToEnumerable = Convert.ToEnumerableFunc(elementType);
                info.ToEnumerableAdapter = Convert.ToEnumerableAdapterFunc(elementType);

                // From ... function
                info.From = InfoFlags.Enumerable;
                info.FromEnumerable = Convert.FromEnumerableFunc(type, elementType);
                return info;
            }

            if (type.IsInterface && enumerableTypes.Length == 1)
            {
                var argument = enumerableTypes[0].GetGenericArguments().Single();
                var collectionDefinitions = new[] { typeof(List<>), typeof(HashSet<>) };
                foreach (var i in collectionDefinitions)
                {
                    var collectionType = i.MakeGenericType(argument);
                    if (type.IsAssignableFrom(collectionType))
                    {
                        var instanceInfo = GetInfo(collectionType);
                        info.ElementType = argument;
                        info.To = InfoFlags.Collection;
                        info.ToCollection = instanceInfo.ToCollection;
                        info.ToCollectionExtend = instanceInfo.ToCollectionExtend;
                        break;
                    }
                }
            }
            else if (type.IsArray)
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
                genericArguments = new[] { elementType };
            }
            else if (genericArguments != null && genericArguments.Length == 1)
            {
                var elementType = genericArguments[0];
                if (Convert.ToFSharpListFunc(type, elementType, out var collectionFunc, out var collectionExtendFunc))
                {
                    info.To = InfoFlags.Collection;
                    info.ToCollection = collectionFunc;
                    info.ToCollectionExtend = collectionExtendFunc;
                }
                else if (genericDefinition == typeof(List<>))
                {
                    info.ElementType = elementType;
                    info.From = InfoFlags.Enumerable;
                    info.To = InfoFlags.Collection; // to list
                    info.FromEnumerable = Convert.FromListFunc(type, elementType);
                    info.ToCollection = Convert.ToListFunc(elementType);
                    info.ToCollectionExtend = Convert.ToListExtendFunc(elementType);
                }
                else if (type.IsAbstract == false && type.IsInterface == false &&
                    interfaces.Contains(typeof(IEnumerable<>).MakeGenericType(elementType)) &&
                    Convert.ToCollectionByConstructorFunc(type, elementType, out collectionFunc, out collectionExtendFunc) ||
                    Convert.ToCollectionByAddFunc(type, elementType, out collectionFunc, out collectionExtendFunc))
                {
                    info.ElementType = elementType;
                    info.To = InfoFlags.Collection;
                    info.ToCollection = collectionFunc;
                    info.ToCollectionExtend = collectionExtendFunc;
                }
            }

            if (info.From == InfoFlags.None && enumerableTypes.Length == 1)
            {
                var enumerableInfo = GetInfo(enumerableTypes[0]);
                info.From = InfoFlags.Enumerable;
                info.ElementType = enumerableInfo.ElementType;
                info.FromEnumerable = enumerableInfo.FromEnumerable;
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
    }
}
