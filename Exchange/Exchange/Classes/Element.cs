using Mikodev.Network.Converters;
using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal struct Element
    {
        internal readonly byte[] buffer;
        internal readonly int offset;
        internal readonly int length;

        internal Element(byte[] buffer)
        {
            this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            offset = 0;
            length = buffer.Length;
        }

        internal Element(byte[] buffer, int offset, int length)
        {
            this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || length < 0 || buffer.Length - offset < length)
                throw new ArgumentOutOfRangeException();
            this.offset = offset;
            this.length = length;
        }

        internal int Limits => offset + length;

        internal object Next(ref int index, PacketConverter converter)
        {
            var offset = index;
            var length = buffer.MoveNextExcept(ref offset, Limits, converter.Length);
            var result = converter.GetObjectChecked(buffer, offset, length);
            index = offset + length;
            return result;
        }

        internal T NextAuto<T>(ref int index, PacketConverter converter)
        {
            var offset = index;
            var length = buffer.MoveNextExcept(ref offset, Limits, converter.Length);
            var result = converter.GetValueCheckedAuto<T>(buffer, offset, length);
            index = offset + length;
            return result;
        }

        internal void ToDictionary<TK, TV>(PacketConverter indexConverter, PacketConverter elementConverter, DictionaryAbstract<TK, TV> dictionary)
        {
            if (length == 0)
                return;
            var keygen = indexConverter as PacketConverter<TK>;
            var valgen = elementConverter as PacketConverter<TV>;
            var keydef = indexConverter.Length;
            var valdef = elementConverter.Length;
            var idx = offset;
            var len = 0;

            try
            {
                while (idx != Limits)
                {
                    len = buffer.MoveNextExcept(ref idx, Limits, keydef);
                    var key = (keygen != null ? keygen.GetValue(buffer, idx, len) : (TK)indexConverter.GetObject(buffer, idx, len));
                    idx += len;

                    len = buffer.MoveNextExcept(ref idx, Limits, valdef);
                    var val = (valgen != null ? valgen.GetValue(buffer, idx, len) : (TV)elementConverter.GetObject(buffer, idx, len));
                    idx += len;

                    dictionary.Add(key, val);
                }
            }
            catch (Exception ex) when (PacketException.ReThrowFilter(ex))
            {
                throw PacketException.ConversionError(ex);
            }
        }

        internal T[] ToArray<T>(PacketConverter converter)
        {
            if (length < 1)
                return new T[0];
            if (typeof(T) == typeof(byte))
                return (T[])(object)UnmanagedArrayConverter<byte>.ToValue(buffer, offset, length);
            else if (typeof(T) == typeof(sbyte))
                return (T[])(object)UnmanagedArrayConverter<sbyte>.ToValue(buffer, offset, length);

            var def = converter.Length;
            var sum = Math.DivRem(length, def, out var rem);
            if (rem != 0)
                throw PacketException.Overflow();
            var arr = new T[sum];
            var gen = converter as PacketConverter<T>;

            try
            {
                if (gen != null)
                    for (int idx = 0; idx < sum; idx++)
                        arr[idx] = gen.GetValue(buffer, offset + idx * def, def);
                else
                    for (int idx = 0; idx < sum; idx++)
                        arr[idx] = (T)converter.GetObject(buffer, offset + idx * def, def);
            }
            catch (Exception ex) when (PacketException.ReThrowFilter(ex))
            {
                throw PacketException.ConversionError(ex);
            }
            return arr;
        }

        internal List<T> ToList<T>(PacketConverter converter)
        {
            if (length < 1)
                return new List<T>();
            if (typeof(T) == typeof(byte))
                return new List<T>((T[])(object)UnmanagedArrayConverter<byte>.ToValue(buffer, offset, length));
            else if (typeof(T) == typeof(sbyte))
                return new List<T>((T[])(object)UnmanagedArrayConverter<sbyte>.ToValue(buffer, offset, length));

            var def = converter.Length;
            var sum = Math.DivRem(length, def, out var rem);
            if (rem != 0)
                throw PacketException.Overflow();
            var lst = new List<T>(sum);
            var gen = converter as PacketConverter<T>;

            try
            {
                if (gen != null)
                    for (int idx = 0; idx < sum; idx++)
                        lst.Add(gen.GetValue(buffer, offset + idx * def, def));
                else
                    for (int idx = 0; idx < sum; idx++)
                        lst.Add((T)converter.GetObject(buffer, offset + idx * def, def));
            }
            catch (Exception ex) when (PacketException.ReThrowFilter(ex))
            {
                throw PacketException.ConversionError(ex);
            }
            return lst;
        }
    }
}
