using System;
using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal class Enumerable : IEnumerable
    {
        protected readonly PacketReader reader;

        protected readonly PacketConverter converter;

        internal Enumerable(PacketReader reader, PacketConverter converter)
        {
            this.reader = reader;
            this.converter = converter;
        }

        private static IEnumerator Enumerator(byte[] buffer, int offset, int count, int define, PacketConverter converter)
        {
            for (int i = 0; i < count; i++)
                yield return converter.GetObjectChecked(buffer, offset + define * i, define);
        }

        private static IEnumerator Enumerator(List<PacketReader> list, PacketConverter converter)
        {
            for (int i = 0; i < list.Count; i++)
                yield return converter.GetObjectChecked(list[i].block);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            var define = converter.Length;
            if (define < 1)
                return Enumerator(reader.GetList(), converter);
            var block = reader.block;
            var quotient = Math.DivRem(block.Length, define, out var remainder);
            if (remainder != 0)
                throw PacketException.Overflow();
            return Enumerator(block.Buffer, block.Offset, quotient, define, converter);
        }
    }

    internal sealed class Enumerable<T> : Enumerable, IEnumerable<T>
    {
        internal Enumerable(PacketReader reader, PacketConverter converter) : base(reader, converter) { }

        private static IEnumerator<T> Enumerator(byte[] buffer, int offset, int count, int define, PacketConverter converter)
        {
            var generic = (PacketConverter<T>)converter;
            for (int i = 0; i < count; i++)
                yield return generic.GetValueChecked(buffer, offset + define * i, define);
        }

        private static IEnumerator<T> Enumerator(List<PacketReader> list, PacketConverter converter)
        {
            var generic = (PacketConverter<T>)converter;
            for (int i = 0; i < list.Count; i++)
                yield return generic.GetValueChecked(list[i].block);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            var define = converter.Length;
            if (define < 1)
                return Enumerator(reader.GetList(), converter);
            var block = reader.block;
            var quotient = Math.DivRem(block.Length, define, out var remainder);
            if (remainder != 0)
                throw PacketException.Overflow();
            else return Enumerator(block.Buffer, block.Offset, quotient, define, converter);
        }
    }
}
