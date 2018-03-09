using System;

namespace Mikodev.Network
{
    partial class PacketConvert
    {
        public static object GetValue(this PacketRawReader reader, Type type)
        {
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(reader);

            var con = _Caches.GetConverter(reader._converters, type, false);
            var val = reader._element.Next(con);
            return val;
        }

        public static T GetValue<T>(this PacketRawReader reader)
        {
            ThrowIfArgumentError(reader);

            var con = _Caches.GetConverter<T>(reader._converters, false);
            var val = reader._element.NextAuto<T>(con);
            return val;
        }

        public static PacketRawWriter SetValue(this PacketRawWriter writer, object value, Type type)
        {
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(writer);

            writer._stream._WriteValue(writer._converters, value, type);
            return writer;
        }

        public static PacketRawWriter SetValue<T>(this PacketRawWriter writer, T value)
        {
            ThrowIfArgumentError(writer);

            writer._stream._WriteValueGeneric(writer._converters, value);
            return writer;
        }
    }
}
