using System;
using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    public static partial class PacketConvert
    {
        internal static void ThrowIfArgumentError(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            return;
        }

        internal static void ThrowIfArgumentError(PacketWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            return;
        }

        internal static void ThrowIfArgumentError(PacketReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            return;
        }

        internal static void ThrowIfArgumentError(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            return;
        }

        internal static void ThrowIfArgumentError(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            return;
        }

        internal static void ThrowIfArgumentError(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || length < 0 || buffer.Length - offset < length)
                throw new ArgumentOutOfRangeException();
            return;
        }

        public static object GetValue(byte[] buffer, Type type)
        {
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(buffer);
            return _Caches.Converter(type, null, false)._GetValueWrapError(buffer, 0, buffer.Length, true);
        }

        public static object GetValue(byte[] buffer, int offset, int length, Type type)
        {
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(buffer, offset, length);
            return _Caches.Converter(type, null, false)._GetValueWrapError(buffer, offset, length, true);
        }
    }
}
