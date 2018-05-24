using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using static Mikodev.Network.Extension;
using ConverterDictionary = System.Collections.Generic.Dictionary<System.Type, Mikodev.Network.PacketConverter>;

namespace Mikodev.Network
{
    internal static partial class Cache
    {
        internal const int Length = 256;

        private static readonly ConcurrentDictionary<Type, Info> s_infos = new ConcurrentDictionary<Type, Info>();
        private static readonly ConcurrentDictionary<Type, GetInfo> s_get_infos = new ConcurrentDictionary<Type, GetInfo>();
        private static readonly ConcurrentDictionary<Type, SetInfo> s_set_infos = new ConcurrentDictionary<Type, SetInfo>();

        internal static void ClearCache()
        {
            s_infos.Clear();
            s_get_infos.Clear();
            s_set_infos.Clear();
        }

#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static PacketConverter GetConverter<T>(ConverterDictionary converters, bool nothrow)
        {
            return GetConverter(converters, typeof(T), nothrow);
        }

        internal static PacketConverter GetConverter(ConverterDictionary converters, Type type, bool nothrow)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (converters != null && converters.TryGetValue(type, out var val))
                if (val == null)
                    goto fail;
                else return val;
            if (s_converters.TryGetValue(type, out val))
                return val;

            var inf = GetInfo(type);
            if (inf.Flag == Info.Enum)
                return s_converters[inf.ElementType];

            fail:
            if (nothrow == true)
                return null;
            throw PacketException.InvalidType(type);
        }

#if NET40 == false
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static Info GetConverterOrInfo(ConverterDictionary converters, Type type, out PacketConverter converter)
        {
            if (converters != null && converters.TryGetValue(type, out converter))
                return null;
            if (s_converters.TryGetValue(type, out converter))
                return null;
            var info = GetInfo(type);
            if (info.Flag != Info.Enum)
                return info;
            converter = s_converters[info.ElementType];
            return null;
        }

        internal static byte[] GetBytes(Type type, ConverterDictionary converters, object value)
        {
            var con = GetConverter(converters, type, false);
            var buf = con.GetBytesWrap(value);
            return buf;
        }

        internal static byte[] GetBytesAuto<T>(ConverterDictionary converters, T value)
        {
            var con = GetConverter<T>(converters, false);
            if (con is PacketConverter<T> res)
                return res.GetBytesWrap(value);
            return con.GetBytesWrap(value);
        }

        internal static byte[][] GetBytesFromEnumerableNonGeneric(PacketConverter converter, IEnumerable enumerable)
        {
            var lst = new List<byte[]>();
            foreach (var i in enumerable)
                lst.Add(converter.GetBytesWrap(i));
            return lst.ToArray();
        }

        internal static byte[][] GetBytesFromArray<T>(PacketConverter converter, T[] array)
        {
            var res = new byte[array.Length][];
            if (converter is PacketConverter<T> gen)
                for (int i = 0; i < array.Length; i++)
                    res[i] = gen.GetBytesWrap(array[i]);
            else
                for (int i = 0; i < array.Length; i++)
                    res[i] = converter.GetBytesWrap(array[i]);
            return res;
        }

        internal static byte[][] GetBytesFromList<T>(PacketConverter converter, List<T> list)
        {
            var res = new byte[list.Count][];
            if (converter is PacketConverter<T> gen)
                for (int i = 0; i < list.Count; i++)
                    res[i] = gen.GetBytesWrap(list[i]);
            else
                for (int i = 0; i < list.Count; i++)
                    res[i] = converter.GetBytesWrap(list[i]);
            return res;
        }

        internal static byte[][] GetBytesFromEnumerable<T>(PacketConverter converter, IEnumerable<T> enumerable)
        {
            if (enumerable is ICollection<T> col && col.Count > 15)
                return GetBytesFromArray(converter, col.ToArray());

            var res = new List<byte[]>();
            if (converter is PacketConverter<T> gen)
                foreach (var i in enumerable)
                    res.Add(gen.GetBytesWrap(i));
            else
                foreach (var i in enumerable)
                    res.Add(converter.GetBytesWrap(i));
            return res.ToArray();
        }

        internal static List<KeyValuePair<byte[], byte[]>> GetBytesFromDictionary<TK, TV>(PacketConverter indexConverter, PacketConverter elementConverter, IEnumerable<KeyValuePair<TK, TV>> enumerable)
        {
            var res = new List<KeyValuePair<byte[], byte[]>>();
            var keygen = indexConverter as PacketConverter<TK>;
            var valgen = elementConverter as PacketConverter<TV>;

            foreach (var i in enumerable)
            {
                var key = i.Key;
                var val = i.Value;
                var keybuf = (keygen != null ? keygen.GetBytesWrap(key) : indexConverter.GetBytesWrap(key));
                var valbuf = (valgen != null ? valgen.GetBytesWrap(val) : elementConverter.GetBytesWrap(val));
                res.Add(new KeyValuePair<byte[], byte[]>(keybuf, valbuf));
            }
            return res;
        }
    }
}
