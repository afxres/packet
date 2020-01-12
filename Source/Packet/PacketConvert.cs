using Mikodev.Network.Internal;
using System;
using System.Text;
using ConverterDictionary = System.Collections.Generic.Dictionary<System.Type, Mikodev.Network.PacketConverter>;

namespace Mikodev.Network
{
    public static partial class PacketConvert
    {
        public static readonly Encoding Encoding = Encoding.UTF8;

        public static readonly bool UseLittleEndian = true;

        #region Throw If Argument Error

        private static void ThrowIfArgumentError(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            return;
        }

        private static void ThrowIfArgumentError(PacketRawWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            return;
        }

        private static void ThrowIfArgumentError(PacketRawReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            return;
        }

        private static void ThrowIfArgumentError(PacketWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException(nameof(writer));
            return;
        }

        private static void ThrowIfArgumentError(PacketReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));
            return;
        }

        private static void ThrowIfArgumentError(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            return;
        }

        private static void ThrowIfArgumentError(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            return;
        }

        private static void ThrowIfArgumentError(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));
            if ((uint)offset > (uint)buffer.Length || (uint)length > (uint)(buffer.Length - offset))
                throw new ArgumentOutOfRangeException();
            return;
        }

        #endregion

        public static object GetValue(byte[] buffer, Type type)
        {
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(buffer);
            return Cache.GetConverter(null, type, false).GetObjectChecked(buffer, 0, buffer.Length, true);
        }

        public static object GetValue(byte[] buffer, int offset, int length, Type type)
        {
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(buffer, offset, length);
            return Cache.GetConverter(null, type, false).GetObjectChecked(buffer, offset, length, true);
        }

        public static object GetValue<T>(byte[] buffer)
        {
            ThrowIfArgumentError(buffer);
            return ((PacketConverter<T>)Cache.GetConverter<T>(null, false)).GetValueChecked(buffer, 0, buffer.Length, true);
        }

        public static object GetValue<T>(byte[] buffer, int offset, int length)
        {
            ThrowIfArgumentError(buffer, offset, length);
            return Cache.GetConverter<T>(null, false).GetObjectChecked(buffer, offset, length, true);
        }

        public static byte[] GetBytes(object value, Type type)
        {
            ThrowIfArgumentError(type);
            return Cache.GetBytes(type, null, value);
        }

        public static byte[] GetBytes<T>(T value)
        {
            return Cache.GetBytes(null, value);
        }

        #region deserialize

        public static object Deserialize(byte[] buffer, Type type, ConverterDictionary converters = null)
        {
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(buffer);

            var rea = new PacketReader(buffer, converters);
            var val = rea.GetValue(type, 0);
            return val;
        }

        public static object Deserialize(byte[] buffer, int offset, int length, Type type, ConverterDictionary converters = null)
        {
            ThrowIfArgumentError(type);
            ThrowIfArgumentError(buffer, offset, length);

            var rea = new PacketReader(buffer, offset, length, converters);
            var val = rea.GetValue(type, 0);
            return val;
        }

        public static T Deserialize<T>(byte[] buffer, ConverterDictionary converters = null)
        {
            return (T)Deserialize(buffer, typeof(T), converters);
        }

        public static T Deserialize<T>(byte[] buffer, int offset, int length, ConverterDictionary converters = null)
        {
            return (T)Deserialize(buffer, offset, length, typeof(T), converters);
        }

        public static T Deserialize<T>(byte[] buffer, T anonymous, ConverterDictionary converters = null)
        {
            return (T)Deserialize(buffer, typeof(T), converters);
        }

        public static T Deserialize<T>(byte[] buffer, int offset, int length, T anonymous, ConverterDictionary converters = null)
        {
            return (T)Deserialize(buffer, offset, length, typeof(T), converters);
        }

        #endregion

        public static byte[] Serialize(object value, ConverterDictionary converters = null)
        {
            var wtr = PacketWriter.GetWriter(converters, value, 0);
            var buf = wtr.GetBytes();
            return buf;
        }

        public static void ClearReflectionCache()
        {
            Cache.ClearCache();
        }
    }
}
