using Mikodev.Network.Converters;
using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal struct Block
    {
        private readonly byte[] buffer;
        private readonly int offset;
        private readonly int length;

        internal Block(Vernier vernier)
        {
            buffer = vernier.Buffer;
            offset = vernier.Offset;
            length = vernier.Length;
        }

        internal Block(byte[] buffer)
        {
            this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            offset = 0;
            length = buffer.Length;
        }

        internal Block(byte[] buffer, int offset, int length)
        {
            this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            if ((uint)offset > (uint)buffer.Length || (uint)length > (uint)(buffer.Length - offset))
                throw new ArgumentOutOfRangeException();
            this.offset = offset;
            this.length = length;
        }

        internal byte[] Buffer => buffer;

        internal int Limits => offset + length;

        internal int Offset => offset;

        internal int Length => length;

        #region to array, to list, to dictionary
        internal void ToDictionary<TK, TV>(PacketConverter indexConverter, PacketConverter elementConverter, DictionaryAbstract<TK, TV> dictionary)
        {
            if (length == 0)
                return;
            var indexGeneric = (PacketConverter<TK>)indexConverter;
            var elementGeneric = (PacketConverter<TV>)elementConverter;
            var vernier = new Vernier(this);

            try
            {
                while (vernier.Any)
                {
                    vernier.FlushExcept(indexGeneric.Length);
                    var key = indexGeneric.GetValue(vernier.Buffer, vernier.Offset, vernier.Length);
                    vernier.FlushExcept(elementGeneric.Length);
                    var value = elementGeneric.GetValue(vernier.Buffer, vernier.Offset, vernier.Length);
                    dictionary.Add(key, value);
                }
            }
            catch (Exception ex) when (PacketException.ReThrowFilter(ex))
            {
                throw PacketException.ConversionError(ex);
            }
        }

        internal T[] ToArray<T>(PacketConverter converter)
        {
            if (Length < 1)
                return new T[0];
            if (typeof(T) == typeof(byte))
                return (T[])(object)UnmanagedArrayConverter<byte>.ToValue(Buffer, Offset, Length);
            else if (typeof(T) == typeof(sbyte))
                return (T[])(object)UnmanagedArrayConverter<sbyte>.ToValue(Buffer, Offset, Length);

            var define = converter.Length;
            var quotient = Math.DivRem(Length, define, out var remainder);
            if (remainder != 0)
                throw PacketException.Overflow();

            try
            {
                var target = new T[quotient];
                var generic = (PacketConverter<T>)converter;
                for (int i = 0; i < quotient; i++)
                    target[i] = generic.GetValue(Buffer, Offset + i * define, define);
                return target;
            }
            catch (Exception ex) when (PacketException.ReThrowFilter(ex))
            {
                throw PacketException.ConversionError(ex);
            }
        }

        internal List<T> ToList<T>(PacketConverter converter)
        {
            if (Length < 1)
                return new List<T>();
            if (typeof(T) == typeof(byte))
                return new List<T>((T[])(object)UnmanagedArrayConverter<byte>.ToValue(Buffer, Offset, Length));
            else if (typeof(T) == typeof(sbyte))
                return new List<T>((T[])(object)UnmanagedArrayConverter<sbyte>.ToValue(Buffer, Offset, Length));

            var define = converter.Length;
            var quotient = Math.DivRem(Length, define, out var remainder);
            if (remainder != 0)
                throw PacketException.Overflow();

            try
            {
                var list = new List<T>(quotient);
                var generic = (PacketConverter<T>)converter;
                for (int i = 0; i < quotient; i++)
                    list.Add(generic.GetValue(Buffer, Offset + i * define, define));
                return list;
            }
            catch (Exception ex) when (PacketException.ReThrowFilter(ex))
            {
                throw PacketException.ConversionError(ex);
            }
        }
        #endregion
    }
}
