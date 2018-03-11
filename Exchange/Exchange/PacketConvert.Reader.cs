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
            var con = _Caches.GetConverter(reader._converters, type, false);
            var val = con._GetValueWrapError(reader._element, true);
            return val;
        }

        public static T GetValue<T>(this PacketReader reader)
        {
            ThrowIfArgumentError(reader);
            var con = _Caches.GetConverter<T>(reader._converters, false);
            var val = con._GetValueWrapErrorAuto<T>(reader._element, true);
            return val;
        }

        public static IEnumerable GetEnumerable(this PacketReader reader, Type type)
        {
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(reader);
            var con = _Caches.GetConverter(reader._converters, type, false);
            return new _Enumerable(reader._element, con);
        }

        public static IEnumerable<T> GetEnumerable<T>(this PacketReader reader)
        {
            ThrowIfArgumentError(reader);
            var con = _Caches.GetConverter<T>(reader._converters, false);
            return new _Enumerable<T>(reader._element, con);
        }

        public static T[] GetArray<T>(this PacketReader reader)
        {
            ThrowIfArgumentError(reader);
            var con = _Caches.GetConverter<T>(reader._converters, false);
            var val = reader._element.Array<T>(con);
            return val;
        }

        public static List<T> GetList<T>(this PacketReader reader)
        {
            ThrowIfArgumentError(reader);
            var con = _Caches.GetConverter<T>(reader._converters, false);
            var val = reader._element.List<T>(con);
            return val;
        }

        public static Dictionary<TK, TV> GetDictionary<TK, TV>(this PacketReader reader)
        {
            ThrowIfArgumentError(reader);
            var key = _Caches.GetConverter<TK>(reader._converters, false);
            var val = _Caches.GetConverter<TV>(reader._converters, false);
            var res = reader._element.Dictionary<TK, TV>(key, val);
            return res;
        }

        public static HashSet<T> GetHashSet<T>(this PacketReader reader)
        {
            ThrowIfArgumentError(reader);
            var con = _Caches.GetConverter<T>(reader._converters, false);
            var col = reader._element.Collection<T>(con);
            var res = new HashSet<T>(col);
            return res;
        }

        public static PacketReader GetItem(this PacketReader reader, string key, bool nothrow = false)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(reader);
            return reader._GetItem(key, nothrow);
        }

        public static PacketReader GetItem(this PacketReader reader, IEnumerable<string> keys, bool nothrow = false)
        {
            ThrowIfArgumentError(reader);
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));
            return reader._GetItem(keys, nothrow);
        }
    }
}
