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
                ? converter.GetObjectChecked(block, true)
                : GetValueMatch(type, level, info);
        }

        internal object GetValueMatch(Type valueType, int level, Info valueInfo)
        {
            PacketException.VerifyRecursionError(ref level);
            switch (valueInfo.To)
            {
                case InfoFlags.Invalid:
                    throw PacketException.InvalidType(valueType);
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
            var indexConverter = Cache.GetConverter(converters, valueInfo.IndexType, true);
            if (indexConverter == null)
                throw PacketException.InvalidKeyType(valueInfo.IndexType, valueInfo.Type);
            var info = Cache.GetConverterOrInfo(converters, valueInfo.ElementType, out var elementConverter);
            if (info == null)
                return valueInfo.ToDictionary(this, indexConverter, elementConverter);

            var collection = new List<object>();
            var vernier = new Vernier(block);
            while (vernier.Any)
            {
                vernier.FlushExcept(indexConverter.Length);
                // Wrap error non-check
                var key = indexConverter.GetObjectChecked(vernier.Buffer, vernier.Offset, vernier.Length);
                vernier.Flush();
                var reader = new PacketReader(new Block(vernier), converters);
                var value = reader.GetValueMatch(valueInfo.ElementType, level, info);
                collection.Add(key);
                collection.Add(value);
            }
            return valueInfo.ToDictionaryExtend(collection);
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
