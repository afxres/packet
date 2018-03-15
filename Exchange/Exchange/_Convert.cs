using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal static class _Convert
    {
        private static List<T> _List<T>(PacketReader rea, IPacketConverter con)
        {
            var itm = rea.GetArray();
            var len = itm.Length;
            if (len < 1)
                return new List<T>();
            var lst = new List<T>(len);
            var gen = con as IPacketConverter<T>;

            try
            {
                if (gen != null)
                    for (int i = 0; i < len; i++)
                        lst.Add(gen.GetValue(itm[i]._ele));
                else
                    for (int i = 0; i < len; i++)
                        lst.Add((T)con.GetValue(itm[i]._ele));
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ThrowConvertError(ex);
            }
            return lst;
        }

        private static T[] _Array<T>(PacketReader rea, IPacketConverter con)
        {
            var itm = rea.GetArray();
            var len = itm.Length;
            if (len < 1)
                return new T[0];
            var arr = new T[len];
            var gen = con as IPacketConverter<T>;

            try
            {
                if (gen != null)
                    for (int i = 0; i < len; i++)
                        arr[i] = gen.GetValue(itm[i]._ele);
                else
                    for (int i = 0; i < len; i++)
                        arr[i] = (T)con.GetValue(itm[i]._ele);
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ThrowConvertError(ex);
            }
            return arr;
        }

        internal static T[] GetArray<T>(PacketReader rea, IPacketConverter con)
        {
            if (con.Length < 1)
                return _Array<T>(rea, con);
            return rea._ele.GetArray<T>(con);
        }

        internal static List<T> GetList<T>(PacketReader rea, IPacketConverter con)
        {
            if (con.Length < 1)
                return _List<T>(rea, con);
            return new List<T>(rea._ele.GetArray<T>(con));
        }

        internal static IEnumerable<T> GetCollection<T>(PacketReader rea, IPacketConverter con)
        {
            if (con.Length < 1)
                return _Array<T>(rea, con);
            return rea._ele.GetArray<T>(con);
        }

        internal static IEnumerable<T> GetEnumerable<T>(PacketReader rea, IPacketConverter con)
        {
            return new _Enumerable<T>(rea, con);
        }

        internal static Dictionary<TK, TV> GetDictionary<TK, TV>(PacketReader rea, IPacketConverter idx, IPacketConverter ele)
        {
            return rea._ele.Dictionary<TK, TV>(idx, ele);
        }

        internal static T[] CastToArray<T>(object[] array)
        {
            var length = array.Length;
            var result = new T[length];
            Array.Copy(array, result, length);
            return result;
        }

        internal static List<T> CastToList<T>(object[] array)
        {
            var values = CastToArray<T>(array);
            var result = new List<T>(values);
            return result;
        }

        internal static Dictionary<TK, TV> CastToDictionary<TK, TV>(IEnumerable<KeyValuePair<object, object>> values)
        {
            var dictionary = new Dictionary<TK, TV>();
            foreach (var i in values)
                dictionary.Add((TK)i.Key, (TV)i.Value);
            return dictionary;
        }
    }
}
