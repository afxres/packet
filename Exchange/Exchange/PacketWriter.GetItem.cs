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
            return info == null ? NewItem(converter.GetBytesWrap(value)) : GetItemMatch(converters, value, level, info);
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
            var ele = valueInfo.ElementType;
            var inf = Cache.GetConverterOrInfo(converters, ele, out var con);
            if (inf == null)
                return NewItem(valueInfo.FromEnumerable(con, value), con.Length);

            var lst = new List<Item>();
            foreach (var i in ((IEnumerable)value))
                lst.Add(GetItemMatch(converters, i, level, inf));
            return NewItem(lst);
        }

        private static Item GetItemMatchDictionary(ConverterDictionary converters, object value, int level, Info valueInfo)
        {
            var key = Cache.GetConverter(converters, valueInfo.IndexType, true);
            if (key == null)
                throw PacketException.InvalidKeyType(valueInfo.IndexType);
            var ele = valueInfo.ElementType;
            var inf = Cache.GetConverterOrInfo(converters, ele, out var con);
            if (inf == null)
                return NewItem(valueInfo.FromDictionary(key, con, value), key.Length, con.Length);

            var lst = new List<KeyValuePair<byte[], Item>>();
            var kvp = valueInfo.FromDictionaryAdapter(key, value);
            foreach (var i in kvp)
            {
                var res = GetItemMatch(converters, i.Value, level, inf);
                var tmp = new KeyValuePair<byte[], Item>(i.Key, res);
                lst.Add(tmp);
            }
            return NewItem(lst, key.Length);
        }

        private static Item GetItemMatchExpando(ConverterDictionary converters, object value, int level)
        {
            var dic = (IDictionary<string, object>)value;
            var lst = new Dictionary<string, PacketWriter>();
            foreach (var i in dic)
                lst[i.Key] = GetWriter(converters, i.Value, level);
            return NewItem(lst);
        }

        private static Item GetItemMatchDefault(ConverterDictionary converters, object value, int level, Info valueInfo)
        {
            var lst = new Dictionary<string, PacketWriter>();
            var get = Cache.GetGetInfo(valueInfo.Type);
            var val = get.GetValues(value);
            var arg = get.Arguments;
            for (int i = 0; i < arg.Length; i++)
                lst[arg[i].Key] = GetWriter(converters, val[i], level);
            return NewItem(lst);
        }
    }
}
