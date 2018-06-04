using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    partial class PacketReader
    {
        internal object GetValue(Type type, int level)
        {
            PacketException.VerifyRecursionError(ref level);
            var info = Cache.GetConverterOrInfo(converters, type, out var converter);
            if (info == null)
                return converter.GetObjectWrap(element, true);
            return GetValueMatch(type, level, info);
        }

        internal object GetValueMatch(Type valueType, int level, Info valueInfo)
        {
            PacketException.VerifyRecursionError(ref level);
            switch (valueInfo.To)
            {
                case InfoFlags.Reader:
                    return this;
                case InfoFlags.RawReader:
                    return new PacketRawReader(this);
                case InfoFlags.Collection:
                    return GetValueMatchCollection(level, valueInfo);
                case InfoFlags.Enumerable:
                    return GetValueMatchEnumerable(level, valueInfo);
                case InfoFlags.Dictionary:
                    return GetValueMatchDictionary(valueType, level, valueInfo);
                default:
                    return GetValueMatchDefault(valueType, level);
            }
        }

        private object GetValueMatchCollection(int level, Info valueInfo)
        {
            var inf = Cache.GetConverterOrInfo(converters, valueInfo.ElementType, out var con);
            if (inf == null)
                return valueInfo.ToCollection(this, con);
            var lst = GetList();
            var len = lst.Count;
            var arr = new object[len];
            for (int i = 0; i < len; i++)
                arr[i] = lst[i].GetValueMatch(valueInfo.ElementType, level, inf);
            var res = valueInfo.ToCollectionExtend(arr);
            return res;
        }

        private object GetValueMatchEnumerable(int level, Info valueInfo)
        {
            var inf = Cache.GetConverterOrInfo(converters, valueInfo.ElementType, out var con);
            if (inf == null)
                return valueInfo.ToEnumerable(this, con);
            return valueInfo.ToEnumerableAdapter(this, inf, level);
        }

        private object GetValueMatchDictionary(Type valueType, int level, Info valueInfo)
        {
            var keycon = Cache.GetConverter(converters, valueInfo.IndexType, true);
            if (keycon == null)
                throw PacketException.InvalidKeyType(valueType);
            var inf = Cache.GetConverterOrInfo(converters, valueInfo.ElementType, out var con);
            if (inf == null)
                return valueInfo.ToDictionary(this, keycon, con);

            var max = element.Limits;
            var idx = element.offset;
            var buf = element.buffer;
            var keydef = keycon.Length;
            var len = 0;

            var lst = new List<object>();
            while (idx != max)
            {
                len = buf.MoveNextExcept(ref idx, max, keydef);
                // Wrap error non-check
                var key = keycon.GetObjectWrap(buf, idx, len);
                idx += len;
                lst.Add(key);

                len = buf.MoveNextExcept(ref idx, max, 0);
                var rea = new PacketReader(buf, idx, len, converters);
                var val = rea.GetValueMatch(valueInfo.ElementType, level, inf);
                idx += len;
                lst.Add(val);
            }
            return valueInfo.ToDictionaryExtend(lst);
        }

        private object GetValueMatchDefault(Type valueType, int level)
        {
            var set = Cache.GetSetInfo(valueType);
            if (set == null)
                throw PacketException.InvalidType(valueType);
            var arg = set.Arguments;
            var arr = new object[arg.Length];
            for (int i = 0; i < arg.Length; i++)
            {
                var rea = GetItem(arg[i].Key, false);
                var val = rea.GetValue(arg[i].Value, level);
                arr[i] = val;
            }

            var res = set.GetObject(arr);
            return res;
        }
    }
}
