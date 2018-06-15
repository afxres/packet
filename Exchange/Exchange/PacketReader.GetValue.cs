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
            return info == null
                ? converter.GetObjectChecked(element, true)
                : GetValueMatch(type, level, info);
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
                    return GetValueMatchDictionary(level, valueInfo);
                default:
                    return GetValueMatchDefault(valueType, level);
            }
        }

        private object GetValueMatchCollection(int level, Info valueInfo)
        {
            var info = Cache.GetConverterOrInfo(converters, valueInfo.ElementType, out var con);
            if (info == null)
                return valueInfo.ToCollection(this, con);
            var list = GetList();
            var length = list.Count;
            var source = new object[length];
            for (int i = 0; i < length; i++)
                source[i] = list[i].GetValueMatch(valueInfo.ElementType, level, info);
            var result = valueInfo.ToCollectionExtend(source);
            return result;
        }

        private object GetValueMatchEnumerable(int level, Info valueInfo)
        {
            var info = Cache.GetConverterOrInfo(converters, valueInfo.ElementType, out var converter);
            return info == null
                ? valueInfo.ToEnumerable(this, converter)
                : valueInfo.ToEnumerableAdapter(this, info, level);
        }

        private object GetValueMatchDictionary(int level, Info valueInfo)
        {
            var keycon = Cache.GetConverter(converters, valueInfo.IndexType, true);
            if (keycon == null)
                throw PacketException.InvalidKeyType(valueInfo.IndexType, valueInfo.Type);
            var info = Cache.GetConverterOrInfo(converters, valueInfo.ElementType, out var con);
            if (info == null)
                return valueInfo.ToDictionary(this, keycon, con);

            var limits = element.Limits;
            var offset = element.offset;
            var buffer = element.buffer;
            var keydef = keycon.Length;
            var length = 0;

            var list = new List<object>();
            while (offset != limits)
            {
                length = buffer.MoveNextExcept(ref offset, limits, keydef);
                // Wrap error non-check
                var key = keycon.GetObjectChecked(buffer, offset, length);
                offset += length;
                list.Add(key);

                length = buffer.MoveNextExcept(ref offset, limits, 0);
                var rea = new PacketReader(buffer, offset, length, converters);
                var val = rea.GetValueMatch(valueInfo.ElementType, level, info);
                offset += length;
                list.Add(val);
            }
            return valueInfo.ToDictionaryExtend(list);
        }

        private object GetValueMatchDefault(Type valueType, int level)
        {
            var set = Cache.GetSetInfo(valueType);
            if (set == null)
                throw PacketException.InvalidType(valueType);
            var arguments = set.Arguments;
            var source = new object[arguments.Length];
            for (int i = 0; i < arguments.Length; i++)
            {
                var reader = GetItem(arguments[i].Key, false);
                var result = reader.GetValue(arguments[i].Value, level);
                source[i] = result;
            }
            var target = set.GetObject(source);
            return target;
        }
    }
}
