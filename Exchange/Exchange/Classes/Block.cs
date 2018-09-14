using Mikodev.Network.Converters;
using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal struct Block
    {
        internal byte[] Buffer { get; }

        internal int Offset { get; }

        internal int Length { get; }

        internal int Limits => Offset + Length;

        internal Block(Vernier vernier)
        {
            Buffer = vernier.Buffer;
            Offset = vernier.Offset;
            Length = vernier.Length;
        }

        internal Block(byte[] buffer)
        {
            Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            Offset = 0;
            Length = buffer.Length;
        }

        internal Block(byte[] buffer, int offset, int length)
        {
            Buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            if ((uint)offset > (uint)buffer.Length || (uint)length > (uint)(buffer.Length - offset))
                throw new ArgumentOutOfRangeException();
            Offset = offset;
            Length = length;
        }

        #region to array, to list, to dictionary
        internal void ToDictionary<TK, TV>(PacketConverter indexConverter, PacketConverter elementConverter, DictionaryAbstract<TK, TV> dictionary)
        {
            if (Length == 0)
                return;
            var indexGeneric = (PacketConverter<TK>)indexConverter;
            var elementGeneric = (PacketConverter<TV>)elementConverter;
            var vernier = (Vernier)this;

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
