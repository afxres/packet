using Mikodev.Network.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using ConverterDictionary = System.Collections.Generic.Dictionary<System.Type, Mikodev.Network.PacketConverter>;

namespace Mikodev.Network
{
    internal static partial class Extension
    {
        internal const int Capacity = 8;

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

        internal static byte[] ToBytes(this ICollection<byte> collection)
        {
            var length = collection?.Count ?? 0;
            if (length == 0)
                return Empty.Array<byte>();
            var target = new byte[length];
            collection.CopyTo(target, 0);
            return target;
        }

        internal static byte[] ToBytes(this ICollection<sbyte> collection)
        {
            var length = collection?.Count ?? 0;
            if (length == 0)
                return Empty.Array<byte>();
            var target = new byte[length];
            var source = new sbyte[length];
            collection.CopyTo(source, 0);
            Unsafe.Copy(ref target[0], in source[0], length);
            return target;
        }

        #region memory stream
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
