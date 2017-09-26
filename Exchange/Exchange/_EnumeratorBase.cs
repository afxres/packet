using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikodev.Network
{
    internal class _EnumeratorBase
    {
        internal readonly int _bit = 0;
        internal readonly int _off = 0;
        internal readonly int _max = 0;
        internal readonly byte[] _buf = null;

        internal int _idx = 0;

        internal _EnumeratorBase(PacketReader source, IPacketConverter converter)
        {
            _buf = source._buf;
            _off = source._off;
            _idx = source._off;
            _max = source._off + source._len;
            _bit = converter.Length ?? -1;
        }

        public void Dispose() { }
    }
}
