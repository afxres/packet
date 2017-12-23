using System;
using System.IO;

namespace Mikodev.Network
{
    partial class PacketWriter
    {
        internal const int _StreamLength = 1024;

        [ThreadStatic]
        internal static WeakReference s_stream;

        internal static MemoryStream _GetStream()
        {
            var val = default(MemoryStream);
            var mst = s_stream;
            if (mst == null)
            {
                val = new MemoryStream(_StreamLength);
                s_stream = new WeakReference(val);
                return val;
            }

            var obj = mst.Target;
            if (obj != null)
            {
                val = (MemoryStream)obj;
                val.SetLength(0);
                return val;
            }

            val = new MemoryStream(_StreamLength);
            mst.Target = val;
            return val;
        }
    }
}
