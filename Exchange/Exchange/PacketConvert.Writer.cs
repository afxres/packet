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
            var itm = writer._GetItems();
            itm[key] = new PacketWriter(writer._cvt) { _itm = val };
            return writer;
        }

        public static PacketWriter SetValue<T>(this PacketWriter writer, string key, T value)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var val = _Caches.GetBytesAuto(writer._cvt, value);
            var itm = writer._GetItems();
            itm[key] = new PacketWriter(writer._cvt) { _itm = val };
            return writer;
        }

        public static PacketWriter SetEnumerable(this PacketWriter writer, string key, ICollection<byte> value)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var val = value._ToBytes();
            var itm = writer._GetItems();
            itm[key] = new PacketWriter(writer._cvt) { _itm = val };
            return writer;
        }

        public static PacketWriter SetEnumerable(this PacketWriter writer, string key, ICollection<sbyte> value)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var val = value._ToBytes();
            var itm = writer._GetItems();
            itm[key] = new PacketWriter(writer._cvt) { _itm = val };
            return writer;
        }

        public static PacketWriter SetEnumerable(this PacketWriter writer, string key, IEnumerable value, Type type)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(writer);

            var sub = new PacketWriter(writer._cvt);
            if (value != null)
                sub._itm = _Caches.GetBytesEnumerable(writer._cvt, value, type);
            var itm = writer._GetItems();
            itm[key] = sub;
            return writer;
        }

        public static PacketWriter SetEnumerable<T>(this PacketWriter writer, string key, IEnumerable<T> value)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var sub = new PacketWriter(writer._cvt);
            if (value != null)
                sub._itm = _Caches.GetBytesEnumerableGeneric(writer._cvt, value);
            var itm = writer._GetItems();
            itm[key] = sub;
            return writer;
        }

        public static PacketWriter SetItem(this PacketWriter writer, string key, PacketWriter another)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var itm = writer._GetItems();
            itm[key] = new PacketWriter(writer._cvt) { _itm = another?._itm };
            return writer;
        }

        public static PacketWriter SetItem(this PacketWriter writer, string key, PacketRawWriter raw)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var itm = writer._GetItems();
            itm[key] = new PacketWriter(writer._cvt) { _itm = raw };
            return writer;
        }
    }
}
