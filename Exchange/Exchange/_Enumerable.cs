using System;
using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal class _Enumerable : IEnumerable
    {
        protected readonly PacketReader rea;
        protected readonly IPacketConverter con;

        internal _Enumerable(PacketReader rea, IPacketConverter con)
        {
            this.rea = rea;
            this.con = con;
        }

        private static IEnumerator Enumerator(byte[] buf, int off, int sum, int def, IPacketConverter con)
        {
            for (int idx = 0; idx < sum; idx++)
                yield return con.GetValueWrap(buf, off + def * idx, def);
        }

        private static IEnumerator Enumerator(PacketReader[] arr, IPacketConverter con)
        {
            for (int idx = 0; idx < arr.Length; idx++)
                yield return con.GetValueWrap(arr[idx]._ele);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            var def = con.Length;
            if (def < 1)
                return Enumerator(rea.GetArray(), con);
            var ele = rea._ele;
            var sum = Math.DivRem(ele._len, def, out var rem);
            if (rem != 0)
                throw PacketException.Overflow();
            return Enumerator(ele._buf, ele._off, sum, def, con);
        }
    }

    internal sealed class _Enumerable<T> : _Enumerable, IEnumerable<T>
    {
        internal _Enumerable(PacketReader rea, IPacketConverter con) : base(rea, con) { }

        private static IEnumerator<T> Enumerator(byte[] buf, int off, int sum, int def, IPacketConverter con)
        {
            if (con is IPacketConverter<T> gen)
                for (int idx = 0; idx < sum; idx++)
                    yield return gen.GetValueWrap(buf, off + def * idx, def);
            else
                for (int idx = 0; idx < sum; idx++)
                    yield return (T)con.GetValueWrap(buf, off + def * idx, def);
        }

        private static IEnumerator<T> Enumerator(PacketReader[] arr, IPacketConverter con)
        {
            if (con is IPacketConverter<T> gen)
                for (int idx = 0; idx < arr.Length; idx++)
                    yield return gen.GetValueWrap(arr[idx]._ele);
            else
                for (int idx = 0; idx < arr.Length; idx++)
                    yield return (T)con.GetValueWrap(arr[idx]._ele);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            var def = con.Length;
            if (def < 1)
                return Enumerator(rea.GetArray(), con);
            var ele = rea._ele;
            var sum = Math.DivRem(ele._len, def, out var rem);
            if (rem != 0)
                throw PacketException.Overflow();
            else return Enumerator(ele._buf, ele._off, sum, def, con);
        }
    }
}
