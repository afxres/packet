﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Mikodev.Network
{
    internal static partial class Convert
    {
        private static List<T> InternalToList<T>(PacketReader reader, PacketConverter converter)
        {
            var itm = reader.GetList();
            var len = itm.Count;
            if (len < 1)
                return new List<T>();
            var lst = new List<T>(len);
            var gen = converter as PacketConverter<T>;

            try
            {
                if (gen != null)
                    for (int i = 0; i < len; i++)
                        lst.Add(gen.GetValue(itm[i].element));
                else
                    for (int i = 0; i < len; i++)
                        lst.Add((T)converter.GetObject(itm[i].element));
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConversionError(ex);
            }
            return lst;
        }

        private static T[] InternalToArray<T>(PacketReader reader, PacketConverter converter)
        {
            var itm = reader.GetList();
            var len = itm.Count;
            if (len < 1)
                return new T[0];
            var arr = new T[len];
            var gen = converter as PacketConverter<T>;

            try
            {
                if (gen != null)
                    for (int i = 0; i < len; i++)
                        arr[i] = gen.GetValue(itm[i].element);
                else
                    for (int i = 0; i < len; i++)
                        arr[i] = (T)converter.GetObject(itm[i].element);
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConversionError(ex);
            }
            return arr;
        }

        internal static T[] ToArray<T>(PacketReader reader, PacketConverter converter)
        {
            if (converter.Length < 1)
                return InternalToArray<T>(reader, converter);
            return reader.element.ToArray<T>(converter);
        }

        internal static List<T> ToList<T>(PacketReader reader, PacketConverter converter)
        {
            if (converter.Length < 1)
                return InternalToList<T>(reader, converter);
            return reader.element.ToList<T>(converter);
        }

        internal static Enumerable<T> ToEnumerable<T>(PacketReader reader, PacketConverter converter)
        {
            return new Enumerable<T>(reader, converter);
        }

        internal static Dictionary<TK, TV> ToDictionary<TK, TV>(PacketReader reader, PacketConverter indexConverter, PacketConverter elementConverter)
        {
            var dic = new DictionaryBuilder<TK, TV>();
            reader.element.ToDictionary(indexConverter, elementConverter, dic);
            return dic.dictionary;
        }

        internal static Dictionary<TK, TV> ToDictionaryExt<TK, TV>(List<object> list)
        {
            var dic = new Dictionary<TK, TV>();
            var idx = 0;
            while (idx < list.Count)
            {
                var key = (TK)list[idx++];
                var val = (TV)list[idx++];
                dic.Add(key, val);
            }
            return dic;
        }

        internal static List<Tuple<TK, TV>> ToTupleList<TK, TV>(PacketReader reader, PacketConverter indexConverter, PacketConverter elementConverter)
        {
            var dic = new TupleListBuilder<TK, TV>();
            reader.element.ToDictionary(indexConverter, elementConverter, dic);
            return dic.tuples;
        }

        internal static List<Tuple<TK, TV>> ToTupleListExt<TK, TV>(List<object> list)
        {
            var lst = new List<Tuple<TK, TV>>();
            var idx = 0;
            while (idx < list.Count)
            {
                var key = (TK)list[idx++];
                var val = (TV)list[idx++];
                lst.Add(new Tuple<TK, TV>(key, val));
            }
            return lst;
        }

        internal static byte[][] FromArray<T>(PacketConverter converter, T[] array)
        {
            var target = new byte[array.Length][];
            if (converter is PacketConverter<T> generic)
                for (int i = 0; i < array.Length; i++)
                    target[i] = generic.GetBytesWrap(array[i]);
            else
                for (int i = 0; i < array.Length; i++)
                    target[i] = converter.GetBytesWrap(array[i]);
            return target;
        }

        internal static byte[][] FromList<T>(PacketConverter converter, List<T> list)
        {
            var target = new byte[list.Count][];
            if (converter is PacketConverter<T> generic)
                for (int i = 0; i < list.Count; i++)
                    target[i] = generic.GetBytesWrap(list[i]);
            else
                for (int i = 0; i < list.Count; i++)
                    target[i] = converter.GetBytesWrap(list[i]);
            return target;
        }

        internal static byte[][] FromEnumerable<T>(PacketConverter converter, IEnumerable<T> enumerable)
        {
            if (enumerable is ICollection<T> collection && collection.Count > 15)
                return FromArray(converter, collection.ToArray());

            var target = new List<byte[]>();
            if (converter is PacketConverter<T> generic)
                foreach (var i in enumerable)
                    target.Add(generic.GetBytesWrap(i));
            else
                foreach (var i in enumerable)
                    target.Add(converter.GetBytesWrap(i));
            return target.ToArray();
        }

        internal static List<KeyValuePair<byte[], byte[]>> FromDictionary<TK, TV>(PacketConverter indexConverter, PacketConverter elementConverter, IEnumerable<KeyValuePair<TK, TV>> enumerable)
        {
            var target = new List<KeyValuePair<byte[], byte[]>>();
            var keyGeneric = indexConverter as PacketConverter<TK>;
            var valGeneric = elementConverter as PacketConverter<TV>;

            foreach (var i in enumerable)
            {
                var key = i.Key;
                var val = i.Value;
                var keyBuffer = (keyGeneric != null ? keyGeneric.GetBytesWrap(key) : indexConverter.GetBytesWrap(key));
                var valBuffer = (valGeneric != null ? valGeneric.GetBytesWrap(val) : elementConverter.GetBytesWrap(val));
                target.Add(new KeyValuePair<byte[], byte[]>(keyBuffer, valBuffer));
            }
            return target;
        }
    }
}