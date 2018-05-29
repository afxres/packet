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
            var assemblyTypes = typeof(Extension).Assembly.GetTypes();
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

        internal static int MoveNext(this byte[] buffer, ref int offset, int limits)
        {
            var cursor = offset;
            if (limits - cursor < sizeof(int))
                return -1;
            var length = UnmanagedConverter<int>.ToValueUnchecked(ref buffer[cursor]);
            cursor += sizeof(int);
            if ((uint)(limits - cursor) < (uint)length)
                return -1;
            offset = cursor;
            return length;
        }

        internal static int MoveNextExcept(this byte[] buffer, ref int offset, int limits, int define)
        {
            if (define > 0)
            {
                if (limits - offset < define)
                    goto fail;
                return define;
            }
            else
            {
                var length = MoveNext(buffer, ref offset, limits);
                if (length < 0)
                    goto fail;
                return length;
            }

            fail:
            throw PacketException.Overflow();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Write(this Stream stream, byte[] buffer) => stream.Write(buffer, 0, buffer.Length);

        internal static void WriteKey(this Stream stream, string key)
        {
            var buffer = Encoding.GetBytes(key);
            var header = UnmanagedConverter<int>.ToBytes(buffer.Length);
            stream.Write(header, 0, header.Length);
            stream.Write(buffer, 0, buffer.Length);
        }

        internal static void WriteExt(this Stream stream, byte[] buffer)
        {
            var header = UnmanagedConverter<int>.ToBytes(buffer.Length);
            stream.Write(header, 0, header.Length);
            stream.Write(buffer, 0, buffer.Length);
        }

        internal static void WriteExt(this Stream stream, MemoryStream other)
        {
            var length = (int)other.Length;
            var header = UnmanagedConverter<int>.ToBytes(length);
            stream.Write(header, 0, sizeof(int));
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
            var converter = Cache.GetConverter(converters, type, false);
            if (converter.Length > 0)
                stream.Write(converter.GetBytesWrap(value));
            else
                stream.WriteExt(converter.GetBytesWrap(value));
        }

        internal static void WriteValueGeneric<T>(this Stream stream, ConverterDictionary converters, T value)
        {
            var converter = Cache.GetConverter<T>(converters, false);
            var generic = converter as PacketConverter<T>;
            if (converter.Length > 0)
            {
                if (generic != null)
                    stream.Write(generic.GetBytesWrap(value));
                else
                    stream.Write(converter.GetBytesWrap(value));
            }
            else
            {
                if (generic != null)
                    stream.WriteExt(generic.GetBytesWrap(value));
                else
                    stream.WriteExt(converter.GetBytesWrap(value));
            }
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
