using System;
using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal class Enumerable : IEnumerable
    {
        protected readonly PacketReader reader;
        protected readonly IPacketConverter converter;

        internal Enumerable(PacketReader reader, IPacketConverter converter)
        {
            this.reader = reader;
            this.converter = converter;
        }

        private static IEnumerator Enumerator(byte[] buf, int off, int sum, int def, IPacketConverter con)
        {
            for (int idx = 0; idx < sum; idx++)
                yield return con.GetValueWrap(buf, off + def * idx, def);
        }

        private static IEnumerator Enumerator(PacketReader[] arr, IPacketConverter con)
        {
            for (int idx = 0; idx < arr.Length; idx++)
                yield return con.GetValueWrap(arr[idx].element);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            var def = converter.Length;
            if (def < 1)
                return Enumerator(reader.GetArray(), converter);
            var ele = reader.element;
            var sum = Math.DivRem(ele.length, def, out var rem);
            if (rem != 0)
                throw PacketException.Overflow();
            return Enumerator(ele.buffer, ele.offset, sum, def, converter);
        }
    }

    internal sealed class Enumerable<T> : Enumerable, IEnumerable<T>
    {
        internal Enumerable(PacketReader rea, IPacketConverter con) : base(rea, con) { }

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
                    yield return gen.GetValueWrap(arr[idx].element);
            else
                for (int idx = 0; idx < arr.Length; idx++)
                    yield return (T)con.GetValueWrap(arr[idx].element);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            var def = converter.Length;
            if (def < 1)
                return Enumerator(reader.GetArray(), converter);
            var ele = reader.element;
            var sum = Math.DivRem(ele.length, def, out var rem);
            if (rem != 0)
                throw PacketException.Overflow();
            else return Enumerator(ele.buffer, ele.offset, sum, def, converter);
        }
    }
}
