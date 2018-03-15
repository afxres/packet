using System;

namespace Mikodev.Network
{
    partial class PacketConvert
    {
        public static object GetValue(this PacketRawReader reader, Type type)
        {
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(reader);

            var con = _Caches.GetConverter(reader._cvt, type, false);
            var val = reader.Next(con);
            return val;
        }

        public static T GetValue<T>(this PacketRawReader reader)
        {
            ThrowIfArgumentError(reader);

            var con = _Caches.GetConverter<T>(reader._cvt, false);
            var val = reader.NextAuto<T>(con);
            return val;
        }

        public static PacketRawWriter SetValue(this PacketRawWriter writer, object value, Type type)
        {
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(writer);

            writer._str.WriteValue(writer._cvt, value, type);
            return writer;
        }

        public static PacketRawWriter SetValue<T>(this PacketRawWriter writer, T value)
        {
            ThrowIfArgumentError(writer);

            writer._str.WriteValueGeneric(writer._cvt, value);
            return writer;
        }
    }
}
