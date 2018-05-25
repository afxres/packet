using Mikodev.Network.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using ConverterDictionary = System.Collections.Generic.Dictionary<System.Type, Mikodev.Network.PacketConverter>;

namespace Mikodev.Network
{
    internal static partial class Extension
    {
        internal static readonly Encoding Encoding = Encoding.UTF8;
        internal static readonly char[] Separator = new[] { '/', '\\' };
        internal static readonly ConverterDictionary Converters;

        static Extension()
        {
            var dictionary = new ConverterDictionary();
            var assembly = typeof(Extension).Assembly;
            var assemblyTypes = assembly.GetTypes();
            var unmanagedConverterType = typeof(UnmanagedConverter<>);
            var unmanagedArrayConverterType = typeof(UnmanagedArrayConverter<>);
            var unmanagedElementTypes = new[]
            {
                typeof(sbyte),
                typeof(char),
                typeof(short),
                typeof(int),
                typeof(long),
                typeof(ushort),
                typeof(uint),
                typeof(ulong),
                typeof(float),
                typeof(double),
            };

            for (int i = 0; i < assemblyTypes.Length; i++)
            {
                var type = assemblyTypes[i];
                var attributes = type.GetCustomAttributes(typeof(ConverterAttribute), false);
                if (attributes.Length != 1)
                    continue;
                var attribute = (ConverterAttribute)attributes[0];
                var elementType = attribute.Type;
                var instance = (PacketConverter)Activator.CreateInstance(type);
                dictionary.Add(elementType, instance);
            }
            for (int i = 0; i < unmanagedElementTypes.Length; i++)
            {
                var elementType = unmanagedElementTypes[i];
                var arrayType = elementType.MakeArrayType();
                if (!dictionary.ContainsKey(elementType))
                {
                    var converterType = unmanagedConverterType.MakeGenericType(elementType);
                    var instance = (PacketConverter)Activator.CreateInstance(converterType);
                    dictionary.Add(elementType, instance);
                }
                if (!dictionary.ContainsKey(arrayType))
                {
                    var converterType = unmanagedArrayConverterType.MakeGenericType(elementType);
                    var instance = (PacketConverter)Activator.CreateInstance(converterType);
                    dictionary.Add(arrayType, instance);
                }
            }
            Converters = dictionary;
        }

        internal static bool MoveNext(this byte[] buffer, int max, ref int index, out int length)
        {
            if (index < 0 || max - index < sizeof(int))
                goto fail;
            length = Unsafe.ReadUnaligned<int>(ref buffer[index]);
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
            var buf = Encoding.GetBytes(key);
            var len = UnmanagedConverter<int>.ToBytes(buf.Length);
            stream.Write(len, 0, len.Length);
            stream.Write(buf, 0, buf.Length);
        }

        internal static void WriteExt(this Stream stream, byte[] buffer)
        {
            var len = UnmanagedConverter<int>.ToBytes(buffer.Length);
            stream.Write(len, 0, len.Length);
            stream.Write(buffer, 0, buffer.Length);
        }

        internal static void WriteExt(this Stream stream, MemoryStream other)
        {
            var len = (int)other.Length;
            var buf = UnmanagedConverter<int>.ToBytes(len);
            stream.Write(buf, 0, sizeof(int));
            other.WriteTo(stream);
        }

        internal static void BeginInternal(this Stream stream, out long source)
        {
            source = stream.Position;
            stream.Position += sizeof(int);
        }

        internal static void FinshInternal(this Stream stream, long source)
        {
            var target = stream.Position;
            var length = target - source - sizeof(int);
            if (length > int.MaxValue)
                throw PacketException.Overflow();
            stream.Position = source;
            var header = UnmanagedConverter<int>.ToBytes((int)length);
            stream.Write(header, 0, header.Length);
            stream.Position = target;
        }

        internal static void WriteValue(this Stream stream, ConverterDictionary converters, object value, Type type)
        {
            var con = Cache.GetConverter(converters, type, false);
            var len = con.Length > 0;
            if (len)
                stream.Write(con.GetBytesWrap(value));
            else
                stream.WriteExt(con.GetBytesWrap(value));
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
                stream.Write(con.GetBytesWrap(value));
            else if (gen != null)
                stream.WriteExt(gen.GetBytesWrap(value));
            else
                stream.WriteExt(con.GetBytesWrap(value));
            return;
        }

        internal static byte[] BorrowOrCopy(byte[] buffer, int offset, int length)
        {
            if (buffer == null)
                goto fail;
            if (offset == 0 && length == buffer.Length)
                return buffer;
            if (offset < 0 || length < 0 || buffer.Length - offset < length)
                goto fail;
            if (length == 0)
                return UnmanagedArrayConverter<byte>.EmptyArray;
            var result = new byte[length];
            Unsafe.CopyBlockUnaligned(ref result[0], ref buffer[offset], (uint)length);
            return result;

            fail:
            throw PacketException.Overflow();
        }

        internal static byte[] ToBytes(this ICollection<byte> collection)
        {
            var length = collection?.Count ?? 0;
            if (length == 0)
                return UnmanagedArrayConverter<byte>.EmptyArray;
            var target = new byte[length];
            collection.CopyTo(target, 0);
            return target;
        }

        internal static byte[] ToBytes(this ICollection<sbyte> collection)
        {
            var length = collection?.Count ?? 0;
            if (length == 0)
                return UnmanagedArrayConverter<byte>.EmptyArray;
            var target = new byte[length];
            var source = new sbyte[length];
            collection.CopyTo(source, 0);
            Unsafe.CopyBlockUnaligned(ref target[0], ref Unsafe.As<sbyte, byte>(ref source[0]), (uint)length);
            return target;
        }
    }
}
