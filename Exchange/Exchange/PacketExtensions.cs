using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
            if (type.IsGenericType == false || type.GetGenericTypeDefinition() != typeof(IEnumerable<>))
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
            foreach (var i in typ.GetInterfaces())
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
        /// Read length from buffer, return false if out if range
        /// </summary>
        internal static bool _Read(this byte[] buffer, ref int offset, out int length, int higher = -1)
        {
            if (higher < 0)
                higher = buffer.Length;
            else if (buffer.Length < higher)
                throw new PacketException(PacketError.AssertFailed);

            if (offset < 0 || higher - offset < sizeof(int))
                goto fail;
            length = ToInt32(buffer, offset);
            offset += sizeof(int);
            if (length < 0 || higher - offset < length)
                goto fail;
            return true;

            fail:
            length = 0;
            return false;
        }

        internal static void _Write(this Stream stream, byte[] buffer) => stream.Write(buffer, 0, buffer.Length);

        internal static void _WriteLen(this Stream stream, int value) => stream._Write(GetBytes(value));

        internal static void _WriteExt(this Stream stream, byte[] buffer)
        {
            var len = GetBytes(buffer.Length);
            stream._Write(len);
            stream._Write(buffer);
        }

        internal static readonly string[] s_seps = new string[] { "/", @"\" };

        internal static readonly Dictionary<Type, PacketConverter> s_cons = new Dictionary<Type, PacketConverter>()
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

            [typeof(TimeSpan)] = new PacketConverter(
                (obj) => GetBytes(((TimeSpan)obj).Ticks),
                (buf, off, len) => new TimeSpan(ToInt64(buf, off)),
                sizeof(long)),

            [typeof(IPAddress)] = new PacketConverter(
                (obj) => ((IPAddress)obj).GetAddressBytes(),
                (buf, off, len) => new IPAddress(buf._Part(off, len)),
                null),

            [typeof(IPEndPoint)] = new PacketConverter(
                (obj) => _OfEndPoint((IPEndPoint)obj),
                _ToEndPoint,
                null),

            [typeof(Guid)] = new PacketConverter(
                (obj) => ((Guid)obj).ToByteArray(),
                (buf, off, len) => new Guid(_Part(buf, off, len)),
                16),

            [typeof(decimal)] = new PacketConverter(
                (obj) => _OfDecimal((decimal)obj),
                (buf, off, len) => _ToDecimal(buf, off, len),
                sizeof(decimal)),

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
        /// Default path separators
        /// </summary>
        public static IReadOnlyList<string> Separators => s_seps;

        /// <summary>
        /// Default packet converters
        /// </summary>
        public static IReadOnlyDictionary<Type, PacketConverter> Converters => s_cons;
    }
}
