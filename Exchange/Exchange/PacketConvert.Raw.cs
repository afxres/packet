using System;

namespace Mikodev.Network
{
    partial class PacketConvert
    {
        public static object GetValue(this PacketRawReader reader, Type type)
        {
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(reader);

            var con = Cache.GetConverter(reader.converters, type, false);
            var val = reader.Next(con);
            return val;
        }

        public static T GetValue<T>(this PacketRawReader reader)
        {
            ThrowIfArgumentError(reader);

            var con = (PacketConverter<T>)Cache.GetConverter<T>(reader.converters, false);
            var val = reader.Next<T>(con);
            return val;
        }

        public static PacketRawWriter SetValue(this PacketRawWriter writer, object value, Type type)
        {
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(writer);

            writer.stream.WriteValue(writer.converters, value, type);
            return writer;
        }

        public static PacketRawWriter SetValue<T>(this PacketRawWriter writer, T value)
        {
            ThrowIfArgumentError(writer);

            writer.stream.WriteValueGeneric(writer.converters, value);
            return writer;
        }
    }
}
