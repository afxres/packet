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

        public static PacketWriter SetValue(this PacketWriter writer, string key, byte[] buffer)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var itm = writer._GetItems();
            itm[key] = new PacketWriter(writer._cvt) { _itm = buffer };
            return writer;
        }

        public static PacketWriter SetValue(this PacketWriter writer, string key, ICollection<byte> buffer)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var len = 0;
            var buf = default(byte[]);
            if (buffer != null && (len = buffer.Count) > 0)
            {
                buf = new byte[len];
                buffer.CopyTo(buf, 0);
            }

            var itm = writer._GetItems();
            itm[key] = new PacketWriter(writer._cvt) { _itm = buf };
            return writer;
        }
        
        public static PacketWriter SetEnumerable(this PacketWriter writer, string key, IEnumerable enumerable, Type type)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(writer);

            var sub = new PacketWriter(writer._cvt);
            if (enumerable != null)
                sub._itm = _Caches.GetBytes(writer._cvt, enumerable, type);
            var itm = writer._GetItems();
            itm[key] = sub;
            return writer;
        }

        public static PacketWriter SetEnumerable<T>(this PacketWriter writer, string key, IEnumerable<T> enumerable)
        {
            ThrowIfArgumentError(key);
            ThrowIfArgumentError(writer);

            var sub = new PacketWriter(writer._cvt);
            if (enumerable != null)
                sub._itm = _Caches.GetBytesGeneric<T>(writer._cvt, enumerable);
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
