using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using static Mikodev.Network.Extension;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    internal static partial class Cache
    {
        internal const int Length = 256;
        internal const int Depth = 64;

        private static readonly ConcurrentDictionary<Type, Info> s_infos = new ConcurrentDictionary<Type, Info>();
        private static readonly ConcurrentDictionary<Type, GetInfo> s_get_infos = new ConcurrentDictionary<Type, GetInfo>();
        private static readonly ConcurrentDictionary<Type, SetInfo> s_set_infos = new ConcurrentDictionary<Type, SetInfo>();

        internal static void ClearCache()
        {
            s_infos.Clear();
            s_get_infos.Clear();
            s_set_infos.Clear();
        }

        internal static IPacketConverter GetConverter<T>(ConverterDictionary dic, bool nothrow)
        {
            return GetConverter(dic, typeof(T), nothrow);
        }

        internal static IPacketConverter GetConverter(ConverterDictionary dic, Type typ, bool nothrow)
        {
            if (typ == null)
                throw new ArgumentNullException(nameof(typ));
            if (dic != null && dic.TryGetValue(typ, out var val))
                if (val == null)
                    goto fail;
                else return val;
            if (s_converters.TryGetValue(typ, out val))
                return val;

            var inf = GetInfo(typ);
            if (inf.Flag == Info.Enum)
                return s_converters[inf.ElementType];

            fail:
            if (nothrow == true)
                return null;
            throw PacketException.InvalidType(typ);
        }

        internal static bool TryGetConverter(ConverterDictionary dic, Type typ, out IPacketConverter val, ref Info inf)
        {
            if (dic != null && dic.TryGetValue(typ, out val))
                return true;
            if (s_converters.TryGetValue(typ, out val))
                return true;
            inf = GetInfo(typ);
            if (inf.Flag != Info.Enum)
                return false;
            val = s_converters[typ];
            return true;
        }

        internal static byte[] GetBytes(Type type, ConverterDictionary dic, object value)
        {
            var con = GetConverter(dic, type, false);
            var buf = con.GetBytesWrap(value);
            return buf;
        }

        internal static byte[] GetBytesAuto<T>(ConverterDictionary dic, T value)
        {
            var con = GetConverter<T>(dic, false);
            if (con is IPacketConverter<T> res)
                return res.GetBytesWrap(value);
            return con.GetBytesWrap(value);
        }

        internal static byte[][] GetBytesFromEnumerableNonGeneric(IPacketConverter con, IEnumerable itr)
        {
            var lst = new List<byte[]>();
            foreach (var i in itr)
                lst.Add(con.GetBytesWrap(i));
            return lst.ToArray();
        }

        internal static byte[][] GetBytesFromArray<T>(IPacketConverter con, T[] arr)
        {
            var res = new byte[arr.Length][];
            if (con is IPacketConverter<T> gen)
                for (int i = 0; i < arr.Length; i++)
                    res[i] = gen.GetBytesWrap(arr[i]);
            else
                for (int i = 0; i < arr.Length; i++)
                    res[i] = con.GetBytesWrap(arr[i]);
            return res;
        }

        internal static byte[][] GetBytesFromList<T>(IPacketConverter con, List<T> arr)
        {
            var res = new byte[arr.Count][];
            if (con is IPacketConverter<T> gen)
                for (int i = 0; i < arr.Count; i++)
                    res[i] = gen.GetBytesWrap(arr[i]);
            else
                for (int i = 0; i < arr.Count; i++)
                    res[i] = con.GetBytesWrap(arr[i]);
            return res;
        }

        internal static byte[][] GetBytesFromEnumerable<T>(IPacketConverter con, IEnumerable<T> itr)
        {
            if (itr is ICollection<T> col && col.Count > 15)
                return GetBytesFromArray(con, col.ToArray());

            var res = new List<byte[]>();
            if (con is IPacketConverter<T> gen)
                foreach (var i in itr)
                    res.Add(gen.GetBytesWrap(i));
            else
                foreach (var i in itr)
                    res.Add(con.GetBytesWrap(i));
            return res.ToArray();
        }

        internal static List<KeyValuePair<byte[], byte[]>> GetBytesFromDictionary<TK, TV>(IPacketConverter keycon, IPacketConverter valcon, IEnumerable<KeyValuePair<TK, TV>> enumerable)
        {
            var res = new List<KeyValuePair<byte[], byte[]>>();
            var keygen = keycon as IPacketConverter<TK>;
            var valgen = valcon as IPacketConverter<TV>;

            foreach (var i in enumerable)
            {
                var key = i.Key;
                var val = i.Value;
                var keybuf = (keygen != null ? keygen.GetBytesWrap(key) : keycon.GetBytesWrap(key));
                var valbuf = (valgen != null ? valgen.GetBytesWrap(val) : valcon.GetBytesWrap(val));
                res.Add(new KeyValuePair<byte[], byte[]>(keybuf, valbuf));
            }
            return res;
        }
    }
}
