using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    partial class _Extension
    {
        internal static byte[] _ToBytes(this ICollection<byte> buffer)
        {
            var len = buffer?.Count ?? 0;
            if (len == 0)
                return s_empty_bytes;
            var buf = new byte[len];
            buffer.CopyTo(buf, 0);
            return buf;
        }

        internal static byte[] _ToBytes(this ICollection<sbyte> buffer)
        {
            var len = buffer?.Count ?? 0;
            if (len == 0)
                return s_empty_bytes;
            var buf = new byte[len];
            var tmp = new sbyte[len];
            buffer.CopyTo(tmp, 0);
            Buffer.BlockCopy(tmp, 0, buf, 0, len);
            return buf;
        }
    }
}
