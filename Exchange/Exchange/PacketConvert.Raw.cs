using System;

namespace Mikodev.Network
{
    partial class PacketConvert
    {
        public static object GetValue(this PacketRawReader reader, Type type)
        {
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(reader);

            var con = _Caches.Converter(type, reader._con, false);
            var val = reader._spa.Next(con);
            return val;
        }

        public static T GetValue<T>(this PacketRawReader reader)
        {
            ThrowIfArgumentError(reader);

            var con = _Caches.Converter<T>(reader._con, false);
            var val = reader._spa.NextAuto<T>(con);
            return val;
        }

        public static PacketRawWriter SetValue(this PacketRawWriter writer, object value, Type type)
        {
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(writer);

            var buf = _Caches.GetBytes(type, writer._dic, value, out var head);
            writer._SetValue(buf, head);
            return writer;
        }

        public static PacketRawWriter SetValue<T>(this PacketRawWriter writer, T value)
        {
            ThrowIfArgumentError(writer);

            var buf = _Caches.GetBytes(writer._dic, value, out var head);
            writer._SetValue(buf, head);
            return writer;
        }
    }
}
