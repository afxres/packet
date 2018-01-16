using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    internal sealed class _Sequence
    {
        private const int _Length = 64;

        private int _len;
        private List<byte[]> _lst;

        internal int Count => _lst.Count;

        internal byte[] GetBytes()
        {
            if (_lst.Count < 1)
                return _Extension.s_empty_bytes;
            var mst = new MemoryStream(_Length);
            GetBytes(mst);
            return mst.ToArray();
        }

        internal void GetBytes(Stream stream)
        {
            if (_len > 0)
                foreach (var i in _lst)
                    stream._Write(i);
            else
                foreach (var i in _lst)
                    if (i != null)
                        stream._WriteExt(i);
            return;
        }

        internal static _Sequence Create(ConverterDictionary dic, IEnumerable itr, Type type)
        {
            var con = _Caches.GetConverter(dic, type, false);
            var lst = new List<byte[]>();
            foreach (var i in itr)
                lst.Add(con._GetBytesWrapError(i));
            var seq = new _Sequence { _len = con.Length, _lst = lst };
            return seq;
        }

        internal static _Sequence CreateGeneric<T>(ConverterDictionary dic, IEnumerable<T> itr)
        {
            var con = _Caches.GetConverter<T>(dic, false);
            var lst = new List<byte[]>();
            if (con is IPacketConverter<T> res)
                foreach (var i in itr)
                    lst.Add(res._GetBytesWrapErrorGeneric(i));
            else
                foreach (var i in itr)
                    lst.Add(con._GetBytesWrapError(i));
            var seq = new _Sequence { _len = con.Length, _lst = lst };
            return seq;
        }

        internal static _Sequence _InternalCreate<T>(IPacketConverter con, IEnumerable itr)
        {
            var lst = new List<byte[]>();
            if (con is IPacketConverter<T> res && itr is IEnumerable<T> col)
                foreach (var i in col)
                    lst.Add(res._GetBytesWrapErrorGeneric(i));
            else
                foreach (var i in itr)
                    lst.Add(con._GetBytesWrapError(i));
            var seq = new _Sequence { _len = con.Length, _lst = lst };
            return seq;
        }
    }
}
