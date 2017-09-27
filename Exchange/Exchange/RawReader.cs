using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mikodev.Network
{
    public class RawReader
    {
        internal _Span _spa;
        internal IReadOnlyDictionary<Type, IPacketConverter> _con;

        public RawReader(PacketReader reader)
        {
            _spa = reader._spa;
            _con = reader._con;
        }

        public object Pull(Type type)
        {
            var con = _Caches.Converter(type, _con, false);
            if (_spa._Next(con.Length, false, out var idx, out var len) == false)
                throw new PacketException(PacketError.Overflow);
            var res = con.GetValue(_spa._buf, idx, len);
            _spa._idx += len;
            return res;
        }
    }
}
