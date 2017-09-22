using System;
using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal class PacketEnumerable : IEnumerable
    {
        internal readonly PacketReader _src = null;
        internal readonly PacketConverter _con = null;

        internal PacketEnumerable(PacketReader reader, Type type)
        {
            if (reader._con.TryGetValue(type, out var con) == false && PacketCaches.TryGetValue(type, out con) == false)
                throw new PacketException(PacketError.TypeInvalid);
            _src = reader;
            _con = con;
        }

        IEnumerator IEnumerable.GetEnumerator() => new PacketEnumerator(_src, _con);
    }

    internal class PacketEnumerable<T> : PacketEnumerable, IEnumerable<T>
    {
        internal PacketEnumerable(PacketReader reader) : base(reader, typeof(T)) { }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => new PacketEnumerator<T>(_src, _con);
    }
}
