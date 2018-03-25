using System;
using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    partial class PacketConvert
    {
        public static object GetValue(this PacketReader reader, Type type)
        {
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(reader);
            var con = _Caches.GetConverter(reader._cvt, type, false);
            var val = con.GetValueWrap(reader._ele, true);
            return val;
        }

        public static T GetValue<T>(this PacketReader reader)
        {
            ThrowIfArgumentError(reader);
            var con = _Caches.GetConverter<T>(reader._cvt, false);
            var val = con.GetValueWrapAuto<T>(reader._ele, true);
            return val;
        }

        public static IEnumerable GetEnumerable(this PacketReader reader, Type type)
        {
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(reader);
            var con = _Caches.GetConverter(reader._cvt, type, false);
            return new _Enumerable(reader, con);
        }

        public static IEnumerable<T> GetEnumerable<T>(this PacketReader reader)
        {
            ThrowIfArgumentError(reader);
            var con = _Caches.GetConverter<T>(reader._cvt, false);
            return new _Enumerable<T>(reader, con);
        }

        public static T[] GetArray<T>(this PacketReader reader)
        {
            ThrowIfArgumentError(reader);
            var con = _Caches.GetConverter<T>(reader._cvt, false);
            var val = _Convert.ToArray<T>(reader, con);
            return val;
        }

        public static List<T> GetList<T>(this PacketReader reader)
        {
            ThrowIfArgumentError(reader);
            var con = _Caches.GetConverter<T>(reader._cvt, false);
            var val = _Convert.ToList<T>(reader, con);
            return val;
        }

        public static Dictionary<TK, TV> GetDictionary<TK, TV>(this PacketReader reader)
        {
            ThrowIfArgumentError(reader);
            var key = _Caches.GetConverter<TK>(reader._cvt, false);
            var val = _Caches.GetConverter<TV>(reader._cvt, false);
            var res = reader._ele.ToDictionary<TK, TV>(key, val);
            return res;
        }

        public static HashSet<T> GetHashSet<T>(this PacketReader reader)
        {
            ThrowIfArgumentError(reader);
            var con = _Caches.GetConverter<T>(reader._cvt, false);
            var col = _Convert.ToCollection<T>(reader, con);
            var res = new HashSet<T>(col);
            return res;
        }

        public static PacketReader GetItem(this PacketReader reader, string key, bool nothrow = false)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(reader);
            return reader.GetItem(key, nothrow);
        }

        public static PacketReader GetItem(this PacketReader reader, IEnumerable<string> keys, bool nothrow = false)
        {
            ThrowIfArgumentError(reader);
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));
            return reader.GetItem(keys, nothrow);
        }
    }
}
