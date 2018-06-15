using System.Collections;
using System.Collections.Generic;
using ConverterDictionary = System.Collections.Generic.Dictionary<System.Type, Mikodev.Network.PacketConverter>;

namespace Mikodev.Network
{
    partial class PacketWriter
    {
        private static Item GetItem(ConverterDictionary converters, object value, int level)
        {
            PacketException.VerifyRecursionError(ref level);
            if (value == null)
                return Item.Empty;
            var type = value.GetType();
            var info = Cache.GetConverterOrInfo(converters, type, out var converter);
            return info == null ? NewItem(converter.GetBytesChecked(value)) : GetItemMatch(converters, value, level, info);
        }

        private static Item GetItemMatch(ConverterDictionary converters, object value, int level, Info valueInfo)
        {
            PacketException.VerifyRecursionError(ref level);
            switch (valueInfo.From)
            {
                case InfoFlags.Writer:
                    return ((PacketWriter)value).item;
                case InfoFlags.RawWriter:
                    return NewItem(((PacketRawWriter)value).stream.ToArray());
                case InfoFlags.Bytes:
                    return NewItem(((ICollection<byte>)value).ToBytes());
                case InfoFlags.SBytes:
                    return NewItem(((ICollection<sbyte>)value).ToBytes());
                case InfoFlags.Enumerable:
                    return GetItemMatchEnumerable(converters, value, level, valueInfo);
                case InfoFlags.Dictionary:
                    return GetItemMatchDictionary(converters, value, level, valueInfo);
                case InfoFlags.Expando:
                    return GetItemMatchExpando(converters, value, level);
                default:
                    return GetItemMatchDefault(converters, value, level, valueInfo);
            }
        }

        private static Item GetItemMatchEnumerable(ConverterDictionary converters, object value, int level, Info valueInfo)
        {
            var elementType = valueInfo.ElementType;
            var info = Cache.GetConverterOrInfo(converters, elementType, out var converter);
            if (info == null)
                return NewItem(valueInfo.FromEnumerable(converter, value), converter.Length);
            if (valueInfo.ElementType == typeof(object))
                throw PacketException.InvalidElementType(typeof(object), valueInfo.Type);
            var list = new List<Item>();
            foreach (var i in ((IEnumerable)value))
                list.Add(GetItemMatch(converters, i, level, info));
            return NewItem(list);
        }

        private static Item GetItemMatchDictionary(ConverterDictionary converters, object value, int level, Info valueInfo)
        {
            var key = Cache.GetConverter(converters, valueInfo.IndexType, true);
            if (key == null)
                throw PacketException.InvalidKeyType(valueInfo.IndexType, valueInfo.Type);
            var elementType = valueInfo.ElementType;
            var info = Cache.GetConverterOrInfo(converters, elementType, out var converter);
            if (info == null)
                return NewItem(valueInfo.FromDictionary(key, converter, value), key.Length, converter.Length);

            var list = new List<KeyValuePair<byte[], Item>>();
            var adapter = valueInfo.FromDictionaryAdapter(key, value);
            if (valueInfo.ElementType == typeof(object))
                foreach (var i in adapter)
                    list.Add(new KeyValuePair<byte[], Item>(i.Key, GetItem(converters, i.Value, level)));
            else
                foreach (var i in adapter)
                    list.Add(new KeyValuePair<byte[], Item>(i.Key, GetItemMatch(converters, i.Value, level, info)));
            return NewItem(list, key.Length);
        }

        private static Item GetItemMatchExpando(ConverterDictionary converters, object value, int level)
        {
            var dictionary = (IDictionary<string, object>)value;
            var list = new Dictionary<string, PacketWriter>();
            foreach (var i in dictionary)
                list[i.Key] = GetWriter(converters, i.Value, level);
            return NewItem(list);
        }

        private static Item GetItemMatchDefault(ConverterDictionary converters, object value, int level, Info valueInfo)
        {
            var dictionary = new Dictionary<string, PacketWriter>();
            var get = Cache.GetGetInfo(valueInfo.Type);
            var values = get.GetValues(value);
            var arguments = get.Arguments;
            for (int i = 0; i < arguments.Length; i++)
                dictionary[arguments[i].Key] = GetWriter(converters, values[i], level);
            return NewItem(dictionary);
        }
    }
}
