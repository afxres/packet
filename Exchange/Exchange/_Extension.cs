using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using static System.BitConverter;

namespace Mikodev.Network
{
    /// <summary>
    /// Extend functions
    /// </summary>
    internal static partial class _Extension
    {
        internal static readonly char[] s_seps = new[] { '/', '\\' };

        static _Extension()
        {
            s_cons = _InitConverter();
        }

        internal static bool _IsEnumerableGeneric(this Type type, out Type inner)
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
                if (i._IsEnumerableGeneric(out inn))
                    return true;
            inn = null;
            return false;
        }

        internal static bool _Read(this byte[] buffer, int higher, ref int offset, out int length)
        {
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

        internal static void _WriteExt(this Stream stream, byte[] buffer, bool header)
        {
            if (header)
                stream._Write(GetBytes(buffer.Length));
            stream._Write(buffer);
        }

        internal static byte[] _Borrow(byte[] buffer, int offset, int length)
        {
            if (offset == 0 && length == buffer.Length)
                return buffer;
            if (offset < 0 || length < 0 || buffer.Length - offset < length)
                throw new PacketException(PacketError.Overflow);
            var buf = new byte[length];
            Buffer.BlockCopy(buffer, offset, buf, 0, length);
            return buf;
        }
    }
}
