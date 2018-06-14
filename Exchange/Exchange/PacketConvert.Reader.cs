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
            var con = Cache.GetConverter(reader.converters, type, false);
            var val = con.GetObjectChecked(reader.element, true);
            return val;
        }

        public static T GetValue<T>(this PacketReader reader)
        {
            ThrowIfArgumentError(reader);
            var con = Cache.GetConverter<T>(reader.converters, false);
            var val = con.GetValueCheckedAuto<T>(reader.element, true);
            return val;
        }

        public static IEnumerable GetEnumerable(this PacketReader reader, Type type)
        {
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(reader);
            var con = Cache.GetConverter(reader.converters, type, false);
            return new Enumerable(reader, con);
        }

        public static IEnumerable<T> GetEnumerable<T>(this PacketReader reader)
        {
            ThrowIfArgumentError(reader);
            var con = Cache.GetConverter<T>(reader.converters, false);
            return new Enumerable<T>(reader, con);
        }

        public static T[] GetArray<T>(this PacketReader reader)
        {
            ThrowIfArgumentError(reader);
            var con = Cache.GetConverter<T>(reader.converters, false);
            var val = Convert.ToArray<T>(reader, con);
            return val;
        }

        public static List<T> GetList<T>(this PacketReader reader)
        {
            ThrowIfArgumentError(reader);
            var con = Cache.GetConverter<T>(reader.converters, false);
            var val = Convert.ToList<T>(reader, con);
            return val;
        }

        public static Dictionary<TK, TV> GetDictionary<TK, TV>(this PacketReader reader)
        {
            ThrowIfArgumentError(reader);
            var key = Cache.GetConverter<TK>(reader.converters, false);
            var val = Cache.GetConverter<TV>(reader.converters, false);
            var res = Convert.ToDictionary<TK, TV>(reader, key, val);
            return res;
        }

        public static HashSet<T> GetHashSet<T>(this PacketReader reader)
        {
            ThrowIfArgumentError(reader);
            var con = Cache.GetConverter<T>(reader.converters, false);
            var col = Convert.ToArray<T>(reader, con);
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
