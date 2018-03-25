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
                throw PacketException.ConvertError(ex);
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
                throw PacketException.ConvertError(ex);
            }
            return arr;
        }

        internal static T[] ToArray<T>(PacketReader rea, IPacketConverter con)
        {
            if (con.Length < 1)
                return _Array<T>(rea, con);
            return rea._ele.ToArray<T>(con);
        }

        internal static List<T> ToList<T>(PacketReader rea, IPacketConverter con)
        {
            if (con.Length < 1)
                return _List<T>(rea, con);
            return new List<T>(rea._ele.ToArray<T>(con));
        }

        internal static IEnumerable<T> ToCollection<T>(PacketReader rea, IPacketConverter con)
        {
            if (con.Length < 1)
                return _Array<T>(rea, con);
            return rea._ele.ToArray<T>(con);
        }

        internal static IEnumerable<T> ToEnumerable<T>(PacketReader rea, IPacketConverter con)
        {
            return new _Enumerable<T>(rea, con);
        }

        internal static Dictionary<TK, TV> ToDictionary<TK, TV>(PacketReader rea, IPacketConverter idx, IPacketConverter ele)
        {
            return rea._ele.ToDictionary<TK, TV>(idx, ele);
        }

        internal static T[] ToArrayCast<T>(object[] arr)
        {
            var len = arr.Length;
            var res = new T[len];
            Array.Copy(arr, res, len);
            return res;
        }

        internal static List<T> ToListCast<T>(object[] arr)
        {
            var val = ToArrayCast<T>(arr);
            var res = new List<T>(val);
            return res;
        }

        internal static Dictionary<TK, TV> ToDictionaryCast<TK, TV>(IEnumerable<KeyValuePair<object, object>> itr)
        {
            var dic = new Dictionary<TK, TV>();
            foreach (var i in itr)
                dic.Add((TK)i.Key, (TV)i.Value);
            return dic;
        }
    }
}
