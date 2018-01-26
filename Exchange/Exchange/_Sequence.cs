using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    internal sealed class _Sequence
    {
        private const int _Length = 256;

        internal readonly List<byte[]> list;
        internal readonly int total;
        internal readonly int define;

        private _Sequence(List<byte[]> list, int total, int define)
        {
            this.list = list;
            this.total = total;
            this.define = define;
        }

        internal int Count => list.Count;

        internal byte[] GetBytes()
        {
            if (list.Count < 1)
                return _Extension.s_empty_bytes;
            var mst = new MemoryStream(_Length);
            WriteTo(mst);
            return mst.ToArray();
        }

        internal void WriteTo(Stream stream)
        {
            foreach (var i in list)
                stream.Write(i, 0, i.Length);
            return;
        }

        internal static _Sequence CreateInternal(IPacketConverter con, IEnumerable itr)
        {
            var lst = new List<byte[]>();
            var def = con.Length;
            var sum = 0L;
            if (def > 0)
            {
                foreach (var i in itr)
                    lst.Add(con._GetBytesWrapError(i));
                sum = lst.Count * def;
            }
            else
            {
                foreach (var i in itr)
                {
                    var buf = con._GetBytesWrapError(i);
                    var len = buf.Length;
                    var pre = (len == 0) ? _Extension.s_zero_bytes : BitConverter.GetBytes(len);
                    lst.Add(pre);
                    lst.Add(buf);
                    sum += (len + sizeof(int));
                }
            }
            if (sum < 0 || sum > int.MaxValue)
                throw new PacketException(PacketError.Overflow);
            var seq = new _Sequence(lst, (int)sum, def);
            return seq;
        }

        internal static _Sequence CreateInternalGeneric<T>(IPacketConverter<T> con, IEnumerable<T> itr)
        {
            var lst = new List<byte[]>();
            var def = con.Length;
            var sum = 0L;
            if (def > 0)
            {
                foreach (var i in itr)
                    lst.Add(con._GetBytesWrapErrorGeneric(i));
                sum = lst.Count * def;
            }
            else
            {
                foreach (var i in itr)
                {
                    var buf = con._GetBytesWrapErrorGeneric(i);
                    var len = buf.Length;
                    var pre = (len == 0) ? _Extension.s_zero_bytes : BitConverter.GetBytes(len);
                    lst.Add(pre);
                    lst.Add(buf);
                    sum += (len + sizeof(int));
                }
            }
            if (sum < 0 || sum > int.MaxValue)
                throw new PacketException(PacketError.Overflow);
            var seq = new _Sequence(lst, (int)sum, def);
            return seq;
        }

        internal static _Sequence CreateInternalAuto<T>(IPacketConverter con, IEnumerable<T> itr)
        {
            if (con is IPacketConverter<T> gen)
                return CreateInternalGeneric(gen, itr);
            return CreateInternal(con, itr);
        }

        internal static _Sequence CreateGeneric<T>(ConverterDictionary dic, IEnumerable<T> itr)
        {
            var con = _Caches.GetConverter<T>(dic, false);
            if (con is IPacketConverter<T> gen)
                return CreateInternalGeneric(gen, itr);
            return CreateInternal(con, itr);
        }

        internal static _Sequence Create(ConverterDictionary dic, IEnumerable itr, Type type)
        {
            var con = _Caches.GetConverter(dic, type, false);
            var seq = CreateInternal(con, itr);
            return seq;
        }
    }
}
