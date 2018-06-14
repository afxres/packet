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
            var valueConverterType = typeof(UnmanagedValueConverter<>);
            var arrayConverterType = typeof(UnmanagedArrayConverter<>);
            var unmanagedTypes = new[]
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
            for (int i = 0; i < unmanagedTypes.Length; i++)
            {
                var elementType = unmanagedTypes[i];
                var arrayType = elementType.MakeArrayType();
                if (!dictionary.ContainsKey(elementType))
                {
                    var converterType = valueConverterType.MakeGenericType(elementType);
                    var instance = (PacketConverter)Activator.CreateInstance(converterType);
                    dictionary.Add(elementType, instance);
                }
                if (!dictionary.ContainsKey(arrayType))
                {
                    var converterType = arrayConverterType.MakeGenericType(elementType);
                    var instance = (PacketConverter)Activator.CreateInstance(converterType);
                    dictionary.Add(arrayType, instance);
                }
            }
            Converters = dictionary;
        }

        internal static int MoveNext(this byte[] buffer, ref int offset, int limits)
        {
            if (limits - offset < sizeof(int))
                return -1;
            var length = UnmanagedValueConverter<int>.ToValueUnchecked(ref buffer[offset]);
            offset += sizeof(int);
            return (uint)(limits - offset) < (uint)length ? -1 : length;
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

        #region memory stream
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Write(this MemoryStream stream, byte[] buffer) => stream.Write(buffer, 0, buffer.Length);

        internal static void WriteExtend(this MemoryStream stream, byte[] buffer)
        {
            var header = UnmanagedValueConverter<int>.ToBytes(buffer.Length);
            stream.Write(header, 0, header.Length);
            stream.Write(buffer, 0, buffer.Length);
        }

        internal static void WriteValue(this MemoryStream stream, ConverterDictionary converters, object value, Type type)
        {
            var converter = Cache.GetConverter(converters, type, false);
            if (converter.Length > 0)
                stream.Write(converter.GetBytesChecked(value));
            else
                stream.WriteExtend(converter.GetBytesChecked(value));
        }

        internal static void WriteValueGeneric<T>(this MemoryStream stream, ConverterDictionary converters, T value)
        {
            var generic = Cache.GetConverter<T>(converters, false);
            if (generic.Length > 0)
                stream.Write(generic.GetBytesChecked(value));
            else
                stream.WriteExtend(generic.GetBytesChecked(value));
        }
        #endregion
    }
}
