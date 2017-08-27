using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Text;
using static System.BitConverter;

namespace Mikodev.Network
{
    /// <summary>
    /// Extend functions
    /// </summary>
    public static partial class PacketExtensions
    {
        internal static bool _IsGenericEnumerable(this Type type, out Type inner)
        {
            if (type.GetTypeInfo().IsGenericType == false || type.GetGenericTypeDefinition() != typeof(IEnumerable<>))
                goto fail;
            var som = type.GetGenericArguments();
            if (som.Length != 1)
                goto fail;
            inner = som[0];
            return true;
            fail:
            inner = null;
            return false;
        }

        internal static bool _IsEnumerable(this Type typ, out Type inn)
        {
            foreach (var i in typ.GetTypeInfo().GetInterfaces())
                if (i._IsGenericEnumerable(out inn))
                    return true;
            inn = null;
            return false;
        }

        internal static byte[] _Part(this byte[] buffer, int offset, int length)
        {
            if (length > buffer.Length)
                throw new PacketException(PacketError.Overflow);
            var buf = new byte[length];
            Buffer.BlockCopy(buffer, offset, buf, 0, length);
            return buf;
        }

        /// <summary>
        /// Return false if any argument out if range
        /// </summary>
        internal static bool _Read(this byte[] buffer, int offset, int length)
        {
            if (offset < 0 || length < 0 || buffer.Length - offset < length)
                return false;
            return true;
        }

        /// <summary>
        /// Read length from buffer, check all arguments
        /// </summary>
        internal static bool _Read(this byte[] buffer, ref int offset, out int length)
        {
            if (buffer._Read(offset, sizeof(int)) == false)
                goto fail;
            length = ToInt32(buffer, offset);
            offset += sizeof(int);
            if (buffer._Read(offset, length) == false)
                goto fail;
            return true;

            fail:
            length = 0;
            return false;
        }

        /// <summary>
        /// Read length from buffer, throw if offset out of range
        /// </summary>
        internal static int _Read(this byte[] buffer, ref int offset)
        {
            if (buffer._Read(offset, sizeof(int)) == false)
                throw new IndexOutOfRangeException();
            var length = ToInt32(buffer, offset);
            offset += sizeof(int);
            return length;
        }

        internal static void _Write(this Stream stream, byte[] buffer) => stream.Write(buffer, 0, buffer.Length);

        internal static void _WriteLen(this Stream stream, int value) => stream._Write(GetBytes(value));

        internal static void _WriteExt(this Stream stream, byte[] buffer)
        {
            var len = GetBytes(buffer.Length);
            stream._Write(len);
            stream._Write(buffer);
        }

        internal static readonly string[] s_Separators = new string[] { "/", @"\" };

        internal static readonly Dictionary<Type, PacketConverter> s_Converters = new Dictionary<Type, PacketConverter>()
        {
            [typeof(byte[])] = new PacketConverter(
                (obj) => (byte[])obj,
                _Part,
                null),

            [typeof(string)] = new PacketConverter(
                (obj) => Encoding.UTF8.GetBytes((string)obj),
                Encoding.UTF8.GetString,
                null),

            [typeof(DateTime)] = new PacketConverter(
                (obj) => GetBytes(((DateTime)obj).ToBinary()),
                (buf, off, len) => DateTime.FromBinary(ToInt64(buf, off)),
                sizeof(long)),

            [typeof(IPAddress)] = new PacketConverter(
                (obj) => ((IPAddress)obj).GetAddressBytes(),
                (buf, off, len) => new IPAddress(buf._Part(off, len)),
                null),

            [typeof(IPEndPoint)] = new PacketConverter(
                (obj) => _EndPointToBinary((IPEndPoint)obj),
                _BinaryToEndPoint,
                null),

            [typeof(bool)] = new PacketConverter(
                (obj) => GetBytes((bool)obj),
                (buf, off, len) => ToBoolean(buf, off),
                sizeof(bool)),

            [typeof(char)] = new PacketConverter(
                (obj) => GetBytes((char)obj),
                (buf, off, len) => ToChar(buf, off),
                sizeof(char)),

            [typeof(sbyte)] = new PacketConverter(
                (obj) => new byte[sizeof(sbyte)] { (byte)(sbyte)obj },
                (buf, off, len) => (sbyte)buf[off],
                sizeof(sbyte)),

            [typeof(byte)] = new PacketConverter(
                (obj) => new byte[sizeof(byte)] { (byte)obj },
                (buf, off, len) => buf[off],
                sizeof(byte)),

            [typeof(short)] = new PacketConverter(
                (obj) => GetBytes((short)obj),
                (buf, off, len) => ToInt16(buf, off),
                sizeof(short)),

            [typeof(ushort)] = new PacketConverter(
                (obj) => GetBytes((ushort)obj),
                (buf, off, len) => ToUInt16(buf, off),
                sizeof(ushort)),

            [typeof(int)] = new PacketConverter(
                (obj) => GetBytes((int)obj),
                (buf, off, len) => ToInt32(buf, off),
                sizeof(int)),

            [typeof(uint)] = new PacketConverter(
                (obj) => GetBytes((uint)obj),
                (buf, off, len) => ToUInt32(buf, off),
                sizeof(uint)),

            [typeof(long)] = new PacketConverter(
                (obj) => GetBytes((long)obj),
                (buf, off, len) => ToInt64(buf, off),
                sizeof(long)),

            [typeof(ulong)] = new PacketConverter(
                (obj) => GetBytes((ulong)obj),
                (buf, off, len) => ToUInt64(buf, off),
                sizeof(ulong)),

            [typeof(float)] = new PacketConverter(
                (obj) => GetBytes((float)obj),
                (buf, off, len) => ToSingle(buf, off),
                sizeof(float)),

            [typeof(double)] = new PacketConverter(
                (obj) => GetBytes((double)obj),
                (buf, off, len) => ToDouble(buf, off),
                sizeof(double)),
        };

        /// <summary>
        /// 默认的路径分隔符
        /// <para>Default path separators</para>
        /// </summary>
        public static IReadOnlyList<string> Separators => s_Separators;

        /// <summary>
        /// Default binary converters
        /// </summary>
        public static IReadOnlyDictionary<Type, PacketConverter> Converters => s_Converters;
    }
}
