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

            var val = _Caches.GetBytes(type, writer._con, value);
            var itm = writer._GetItems();
            itm[key] = new PacketWriter(writer._con) { _obj = val };
            return writer;
        }

        public static PacketWriter SetValue<T>(this PacketWriter writer, string key, T value)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var val = _Caches.GetBytes(writer._con, value);
            var itm = writer._GetItems();
            itm[key] = new PacketWriter(writer._con) { _obj = val };
            return writer;
        }

        public static PacketWriter SetValue(this PacketWriter writer, string key, byte[] buffer)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var itm = writer._GetItems();
            itm[key] = new PacketWriter(writer._con) { _obj = buffer };
            return writer;
        }

        public static PacketWriter SetEnumerable(this PacketWriter writer, string key, Type type, IEnumerable enumerable)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(writer);

            var nod = new PacketWriter(writer._con);
            if (enumerable != null)
                nod._GetBytes(type, enumerable);
            var itm = writer._GetItems();
            itm[key] = nod;
            return writer;
        }

        public static PacketWriter SetEnumerable<T>(this PacketWriter writer, string key, IEnumerable<T> enumerable)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var nod = new PacketWriter(writer._con);
            if (enumerable != null)
                nod._GetBytes(enumerable);
            var itm = writer._GetItems();
            itm[key] = nod;
            return writer;
        }

        public static PacketWriter SetItem(this PacketWriter writer, string key, PacketWriter another)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var itm = writer._GetItems();
            itm[key] = new PacketWriter(writer._con) { _obj = another?._obj };
            return writer;
        }

        public static PacketWriter SetItem(this PacketWriter writer, string key, PacketRawWriter raw)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var itm = writer._GetItems();
            itm[key] = raw;
            return writer;
        }
    }
}
