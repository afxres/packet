using Mikodev.Network.Tokens;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    public partial class PacketConvert
    {
        public static PacketWriter SetValue(this PacketWriter writer, string key, object value, Type type)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(writer);

            var val = Cache.GetBytes(type, writer.converters, value);
            var itm = writer.GetDictionary();
            itm[key] = new PacketWriter(writer.converters, new Value(val));
            return writer;
        }

        public static PacketWriter SetValue<T>(this PacketWriter writer, string key, T value)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var val = Cache.GetBytes(writer.converters, value);
            var itm = writer.GetDictionary();
            itm[key] = new PacketWriter(writer.converters, new Value(val));
            return writer;
        }

        public static PacketWriter SetEnumerable(this PacketWriter writer, string key, ICollection<byte> value)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var val = value.ToBytes();
            var itm = writer.GetDictionary();
            itm[key] = new PacketWriter(writer.converters, new Value(val));
            return writer;
        }

        public static PacketWriter SetEnumerable(this PacketWriter writer, string key, ICollection<sbyte> value)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var val = value.ToBytes();
            var itm = writer.GetDictionary();
            itm[key] = new PacketWriter(writer.converters, new Value(val));
            return writer;
        }

        public static PacketWriter SetEnumerable(this PacketWriter writer, string key, IEnumerable value, Type type)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(writer);

            var con = Cache.GetConverter(writer.converters, type, false);
            var val = (value == null ? null : Cache.GetBytesFromEnumerableNonGeneric(con, value));
            var sub = new PacketWriter(writer.converters, new ValueArray(val, con.Length));
            var itm = writer.GetDictionary();
            itm[key] = sub;
            return writer;
        }

        public static PacketWriter SetEnumerable<T>(this PacketWriter writer, string key, IEnumerable<T> value)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var converter = Cache.GetConverter<T>(writer.converters, false);
            var result = (value != null)
                ? (value is T[] array)
                    ? Convert.FromArray(converter, array)
                    : (value is List<T> list)
                        ? Convert.FromList(converter, list)
                        : Convert.FromEnumerable(converter, value)
                : null;
            var sub = new PacketWriter(writer.converters, new ValueArray(result, converter.Length));
            var itm = writer.GetDictionary();
            itm[key] = sub;
            return writer;
        }

        public static PacketWriter SetDictionary<TK, TV>(this PacketWriter writer, string key, IDictionary<TK, TV> value)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var keycon = Cache.GetConverter<TK>(writer.converters, false);
            var valcon = Cache.GetConverter<TV>(writer.converters, false);
            var val = (value == null ? null : Convert.FromDictionary(keycon, valcon, value));
            var sub = new PacketWriter(writer.converters, new ValueDictionary(val, keycon.Length, valcon.Length));
            var itm = writer.GetDictionary();
            itm[key] = sub;
            return writer;
        }

        public static PacketWriter SetItem(this PacketWriter writer, string key, PacketWriter another)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var itm = writer.GetDictionary();
            itm[key] = new PacketWriter(writer.converters, another);
            return writer;
        }

        public static PacketWriter SetItem(this PacketWriter writer, string key, PacketRawWriter raw)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var itm = writer.GetDictionary();
            itm[key] = new PacketWriter(writer.converters, new Value(raw?.stream.ToArray()));
            return writer;
        }
    }
}
