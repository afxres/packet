using Mikodev.Network.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ConverterDictionary = System.Collections.Generic.Dictionary<System.Type, Mikodev.Network.PacketConverter>;

namespace Mikodev.Network
{
    internal static partial class Extension
    {
        internal static readonly Encoding s_encoding = Encoding.UTF8;

        internal static readonly byte[] s_empty_bytes = new byte[0];

        internal static readonly char[] s_separators = new[] { '/', '\\' };

        internal static readonly ConverterDictionary s_converters = null;

        static Extension()
        {
            var dictionary = new ConverterDictionary();
            var assembly = typeof(Extension).Assembly;

            foreach (var t in assembly.GetTypes())
            {
                var attributes = t.GetCustomAttributes(typeof(PacketConverterAttribute), false);
                if (attributes.Length != 1)
                    continue;
                var attribute = (PacketConverterAttribute)attributes[0];
                var type = attribute.Type;
                var instance = (PacketConverter)Activator.CreateInstance(t);
                dictionary.Add(type, instance);
            }
            s_converters = dictionary;
        }

        internal static bool MoveNext(this byte[] buffer, int max, ref int index, out int length)
        {
            if (index < 0 || max - index < sizeof(int))
                goto fail;
            length = BitConverter.ToInt32(buffer, index);
            index += sizeof(int);
            if (length < 0 || max - index < length)
                goto fail;
            return true;

            fail:
            length = 0;
            return false;
        }

        internal static void Write(this Stream stream, byte[] buffer) => stream.Write(buffer, 0, buffer.Length);

        internal static void WriteKey(this Stream stream, string key)
        {
            var buf = s_encoding.GetBytes(key);
            var len = BitConverter.GetBytes(buf.Length);
            stream.Write(len, 0, len.Length);
            stream.Write(buf, 0, buf.Length);
        }

        internal static void WriteExt(this Stream stream, byte[] buffer)
        {
            var len = BitConverter.GetBytes(buffer.Length);
            stream.Write(len, 0, len.Length);
            stream.Write(buffer, 0, buffer.Length);
        }

        internal static void WriteExt(this Stream stream, MemoryStream other)
        {
            var len = (int)other.Length;
            var buf = BitConverter.GetBytes(len);
            stream.Write(buf, 0, sizeof(int));
            other.WriteTo(stream);
        }

        internal static byte[] Span(byte[] buffer, int offset, int length)
        {
            if (offset == 0 && length == buffer.Length)
                return buffer;
            if (offset < 0 || length < 0 || buffer.Length - offset < length)
                throw PacketException.Overflow();
            if (length == 0)
                return s_empty_bytes;
            var buf = new byte[length];
            Buffer.BlockCopy(buffer, offset, buf, 0, length);
            return buf;
        }

        internal static void BeginInternal(this Stream stream, out long source)
        {
            var pos = stream.Position;
            stream.Position += sizeof(int);
            source = pos;
        }

        internal static void FinshInternal(this Stream stream, long source)
        {
            var dst = stream.Position;
            var len = dst - source - sizeof(int);
            if (len > int.MaxValue)
                throw PacketException.Overflow();
            stream.Position = source;
            var buf = BitConverter.GetBytes((int)len);
            stream.Write(buf, 0, buf.Length);
            stream.Position = dst;
        }

        internal static void WriteValue(this Stream stream, ConverterDictionary converters, object value, Type type)
        {
            var con = Cache.GetConverter(converters, type, false);
            var len = con.Length > 0;
            if (len)
                stream.Write(con.GetBufferWrap(value));
            else
                stream.WriteExt(con.GetBufferWrap(value));
            return;
        }

        internal static void WriteValueGeneric<T>(this Stream stream, ConverterDictionary converters, T value)
        {
            var con = Cache.GetConverter<T>(converters, false);
            var len = con.Length > 0;
            var gen = con as PacketConverter<T>;
            if (len && gen != null)
                stream.Write(gen.GetBytesWrap(value));
            else if (len)
                stream.Write(con.GetBufferWrap(value));
            else if (gen != null)
                stream.WriteExt(gen.GetBytesWrap(value));
            else
                stream.WriteExt(con.GetBufferWrap(value));
            return;
        }

        internal static byte[] ToBytes(this ICollection<byte> collection)
        {
            var len = collection?.Count ?? 0;
            if (len == 0)
                return s_empty_bytes;
            var buf = new byte[len];
            collection.CopyTo(buf, 0);
            return buf;
        }

        internal static byte[] ToBytes(this ICollection<sbyte> collection)
        {
            var len = collection?.Count ?? 0;
            if (len == 0)
                return s_empty_bytes;
            var buf = new byte[len];
            var tmp = new sbyte[len];
            collection.CopyTo(tmp, 0);
            Buffer.BlockCopy(tmp, 0, buf, 0, len);
            return buf;
        }
    }
}
