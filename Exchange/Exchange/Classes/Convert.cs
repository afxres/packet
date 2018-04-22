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
            return new List<T>(reader.element.ToArray<T>(converter));
        }

        internal static IEnumerable<T> ToCollection<T>(PacketReader reader, IPacketConverter converter)
        {
            if (converter.Length < 1)
                return GetArray<T>(reader, converter);
            return reader.element.ToArray<T>(converter);
        }

        internal static IEnumerable<T> ToEnumerable<T>(PacketReader reader, IPacketConverter converter)
        {
            return new Enumerable<T>(reader, converter);
        }

        internal static Dictionary<TK, TV> ToDictionary<TK, TV>(PacketReader reader, IPacketConverter indexConverter, IPacketConverter elementConverter)
        {
            return reader.element.ToDictionary<TK, TV>(indexConverter, elementConverter);
        }

        internal static T[] ToArrayCast<T>(object[] array)
        {
            var len = array.Length;
            var res = new T[len];
            Array.Copy(array, res, len);
            return res;
        }

        internal static List<T> ToListCast<T>(object[] array)
        {
            var val = ToArrayCast<T>(array);
            var res = new List<T>(val);
            return res;
        }

        internal static Dictionary<TK, TV> ToDictionaryCast<TK, TV>(IEnumerable<KeyValuePair<object, object>> dictionary)
        {
            var dic = new Dictionary<TK, TV>();
            foreach (var i in dictionary)
                dic.Add((TK)i.Key, (TV)i.Value);
            return dic;
        }
    }
}
