using System;
using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    partial class PacketConvert
    {
        public static PacketWriter SetValue(this PacketWriter writer, string key, object value, Type type)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(writer);

            var val = _Caches.GetBytes(type, writer._cvt, value);
            var itm = writer.GetDictionary();
            itm[key] = new PacketWriter(writer._cvt, new PacketWriter.Item(val));
            return writer;
        }

        public static PacketWriter SetValue<T>(this PacketWriter writer, string key, T value)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var val = _Caches.GetBytesAuto(writer._cvt, value);
            var itm = writer.GetDictionary();
            itm[key] = new PacketWriter(writer._cvt, new PacketWriter.Item(val));
            return writer;
        }

        public static PacketWriter SetEnumerable(this PacketWriter writer, string key, ICollection<byte> value)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var val = value.ToBytes();
            var itm = writer.GetDictionary();
            itm[key] = new PacketWriter(writer._cvt, new PacketWriter.Item(val));
            return writer;
        }

        public static PacketWriter SetEnumerable(this PacketWriter writer, string key, ICollection<sbyte> value)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var val = value.ToBytes();
            var itm = writer.GetDictionary();
            itm[key] = new PacketWriter(writer._cvt, new PacketWriter.Item(val));
            return writer;
        }

        public static PacketWriter SetEnumerable(this PacketWriter writer, string key, IEnumerable value, Type type)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(writer);

            var con = _Caches.GetConverter(writer._cvt, type, false);
            var val = (value == null ? null : _Caches.GetBytesFromEnumerableNonGeneric(con, value));
            var sub = new PacketWriter(writer._cvt, new PacketWriter.Item(val, con.Length));
            var itm = writer.GetDictionary();
            itm[key] = sub;
            return writer;
        }

        public static PacketWriter SetEnumerable<T>(this PacketWriter writer, string key, IEnumerable<T> value)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var con = _Caches.GetConverter<T>(writer._cvt, false);
            var val = default(byte[][]);
            if (value != null)
            {
                if (value is T[] arr)
                    val = _Caches.GetBytesFromArray(con, arr);
                else if (value is List<T> lst)
                    val = _Caches.GetBytesFromList(con, lst);
                else
                    val = _Caches.GetBytesFromEnumerable(con, value);
            }
            var sub = new PacketWriter(writer._cvt, new PacketWriter.Item(val, con.Length));
            var itm = writer.GetDictionary();
            itm[key] = sub;
            return writer;
        }

        public static PacketWriter SetDictionary<TK, TV>(this PacketWriter writer, string key, IDictionary<TK, TV> value)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var keycon = _Caches.GetConverter<TK>(writer._cvt, false);
            var valcon = _Caches.GetConverter<TV>(writer._cvt, false);
            var val = (value == null ? null : _Caches.GetBytesFromDictionary(keycon, valcon, value));
            var sub = new PacketWriter(writer._cvt, new PacketWriter.Item(val, keycon.Length, valcon.Length));
            var itm = writer.GetDictionary();
            itm[key] = sub;
            return writer;
        }

        public static PacketWriter SetItem(this PacketWriter writer, string key, PacketWriter another)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var itm = writer.GetDictionary();
            itm[key] = new PacketWriter(writer._cvt, another);
            return writer;
        }

        public static PacketWriter SetItem(this PacketWriter writer, string key, PacketRawWriter raw)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var itm = writer.GetDictionary();
            itm[key] = new PacketWriter(writer._cvt, new PacketWriter.Item(raw?._str));
            return writer;
        }
    }
}
