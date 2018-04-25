using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal static class Convert
    {
        private static List<T> GetList<T>(PacketReader reader, IPacketConverter converter)
        {
            var itm = reader.GetArray();
            var len = itm.Length;
            if (len < 1)
                return new List<T>();
            var lst = new List<T>(len);
            var gen = converter as IPacketConverter<T>;

            try
            {
                if (gen != null)
                    for (int i = 0; i < len; i++)
                        lst.Add(gen.GetValue(itm[i].element));
                else
                    for (int i = 0; i < len; i++)
                        lst.Add((T)converter.GetValue(itm[i].element));
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConvertError(ex);
            }
            return lst;
        }

        private static T[] GetArray<T>(PacketReader reader, IPacketConverter converter)
        {
            var itm = reader.GetArray();
            var len = itm.Length;
            if (len < 1)
                return new T[0];
            var arr = new T[len];
            var gen = converter as IPacketConverter<T>;

            try
            {
                if (gen != null)
                    for (int i = 0; i < len; i++)
                        arr[i] = gen.GetValue(itm[i].element);
                else
                    for (int i = 0; i < len; i++)
                        arr[i] = (T)converter.GetValue(itm[i].element);
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConvertError(ex);
            }
            return arr;
        }

        internal static T[] ToArray<T>(PacketReader reader, IPacketConverter converter)
        {
            if (converter.Length < 1)
                return GetArray<T>(reader, converter);
            return reader.element.ToArray<T>(converter);
        }

        internal static List<T> ToList<T>(PacketReader reader, IPacketConverter converter)
        {
            if (converter.Length < 1)
                return GetList<T>(reader, converter);
            return reader.element.ToList<T>(converter);
        }

        internal static Enumerable<T> ToEnumerable<T>(PacketReader reader, IPacketConverter converter)
        {
            return new Enumerable<T>(reader, converter);
        }

        internal static Dictionary<TK, TV> ToDictionary<TK, TV>(PacketReader reader, IPacketConverter indexConverter, IPacketConverter elementConverter)
        {
            var dic = new DictionaryBuilder<TK, TV>();
            reader.element.ToDictionary(indexConverter, elementConverter, dic);
            return dic.dictionary;
        }

        internal static Dictionary<TK, TV> ToDictionaryCast<TK, TV>(List<object> list)
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

        internal static List<Tuple<TK, TV>> ToTupleList<TK, TV>(PacketReader reader, IPacketConverter indexConverter, IPacketConverter elementConverter)
        {
            var dic = new TupleListBuilder<TK, TV>();
            reader.element.ToDictionary(indexConverter, elementConverter, dic);
            return dic.tuples;
        }

        internal static List<Tuple<TK, TV>> ToTupleListCast<TK, TV>(List<object> list)
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
    }
}
