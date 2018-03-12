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

            var val = _Caches.GetBytes(type, writer._converters, value);
            var itm = writer._GetItemDictionary();
            itm[key] = new PacketWriter(writer._converters) { _item = val };
            return writer;
        }

        public static PacketWriter SetValue<T>(this PacketWriter writer, string key, T value)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var val = _Caches.GetBytesAuto(writer._converters, value);
            var itm = writer._GetItemDictionary();
            itm[key] = new PacketWriter(writer._converters) { _item = val };
            return writer;
        }

        public static PacketWriter SetEnumerable(this PacketWriter writer, string key, ICollection<byte> value)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var val = value._ToBytes();
            var itm = writer._GetItemDictionary();
            itm[key] = new PacketWriter(writer._converters) { _item = val };
            return writer;
        }

        public static PacketWriter SetEnumerable(this PacketWriter writer, string key, ICollection<sbyte> value)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var val = value._ToBytes();
            var itm = writer._GetItemDictionary();
            itm[key] = new PacketWriter(writer._converters) { _item = val };
            return writer;
        }

        public static PacketWriter SetEnumerable(this PacketWriter writer, string key, IEnumerable value, Type type)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(writer);

            var sub = new PacketWriter(writer._converters);
            if (value != null)
                sub._item = _Caches.GetStream(writer._converters, value, type);
            var itm = writer._GetItemDictionary();
            itm[key] = sub;
            return writer;
        }

        public static PacketWriter SetEnumerable<T>(this PacketWriter writer, string key, IEnumerable<T> value)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var sub = new PacketWriter(writer._converters);
            if (value != null)
                sub._item = _Caches.GetStreamGeneric(writer._converters, value);
            var itm = writer._GetItemDictionary();
            itm[key] = sub;
            return writer;
        }

        public static PacketWriter SetDictionary<TK, TV>(this PacketWriter writer, string key, IDictionary<TK, TV> value)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var sub = new PacketWriter(writer._converters);
            if (value != null)
                sub._item = _Caches.GetStreamGeneric(writer._converters, value);
            var itm = writer._GetItemDictionary();
            itm[key] = sub;
            return writer;
        }

        public static PacketWriter SetItem(this PacketWriter writer, string key, PacketWriter another)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var itm = writer._GetItemDictionary();
            itm[key] = new PacketWriter(writer._converters) { _item = another?._item };
            return writer;
        }

        public static PacketWriter SetItem(this PacketWriter writer, string key, PacketRawWriter raw)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var itm = writer._GetItemDictionary();
            itm[key] = new PacketWriter(writer._converters) { _item = raw._stream };
            return writer;
        }
    }
}
