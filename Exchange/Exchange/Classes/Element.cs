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

        internal int Max => offset + length;

        internal void MoveNext(int define, ref int index, out int length)
        {
            var max = Max;
            if ((define > 0 && index + define > max) || (define < 1 && buffer.MoveNext(max, ref index, out define) == false))
                throw PacketException.Overflow();
            length = define;
        }

        internal object Next(ref int index, PacketConverter converter)
        {
            var tmp = index;
            MoveNext(converter.Length, ref tmp, out var len);
            var res = converter.GetObjectWrap(buffer, tmp, len);
            index = tmp + len;
            return res;
        }

        internal T NextAuto<T>(ref int index, PacketConverter converter)
        {
            var tmp = index;
            MoveNext(converter.Length, ref tmp, out var len);
            var res = converter.GetValueWrapAuto<T>(buffer, tmp, len);
            index = tmp + len;
            return res;
        }

        internal void ToDictionary<TK, TV>(PacketConverter indexConverter, PacketConverter elementConverter, DictionaryAbstract<TK, TV> dictionary)
        {
            if (length == 0)
                return;
            var keygen = indexConverter as PacketConverter<TK>;
            var valgen = elementConverter as PacketConverter<TV>;
            var keylen = indexConverter.Length;
            var vallen = elementConverter.Length;
            var max = Max;
            var idx = offset;
            var len = 0;

            try
            {
                while (true)
                {
                    var sub = max - idx;
                    if (sub == 0)
                        break;

                    if (keylen > 0)
                        if (sub < keylen)
                            goto fail;
                        else
                            len = keylen;
                    else if (buffer.MoveNext(max, ref idx, out len) == false)
                        goto fail;

                    var key = (keygen != null ? keygen.GetValue(buffer, idx, len) : (TK)indexConverter.GetObject(buffer, idx, len));
                    idx += len;
                    sub = max - idx;

                    if (vallen > 0)
                        if (sub < vallen)
                            goto fail;
                        else
                            len = vallen;
                    else if (buffer.MoveNext(max, ref idx, out len) == false)
                        goto fail;

                    var val = (valgen != null ? valgen.GetValue(buffer, idx, len) : (TV)elementConverter.GetObject(buffer, idx, len));
                    idx += len;
                    dictionary.Add(key, val);
                }
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConvertError(ex);
            }

            return;
            fail:
            throw PacketException.Overflow();
        }

        private object GetByteArray() => ByteArrayConverter.ToValue(buffer, offset, length);

        private object GetSByteArray() => SByteArrayConverter.ToValue(buffer, offset, length);

        internal T[] ToArray<T>(PacketConverter converter)
        {
            if (length < 1)
                return new T[0];
            if (typeof(T) == typeof(byte))
                return (T[])GetByteArray();
            else if (typeof(T) == typeof(sbyte))
                return (T[])GetSByteArray();

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
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConvertError(ex);
            }
            return arr;
        }

        internal List<T> ToList<T>(PacketConverter converter)
        {
            if (length < 1)
                return new List<T>();
            if (typeof(T) == typeof(byte))
                return new List<T>((T[])GetByteArray());
            else if (typeof(T) == typeof(sbyte))
                return new List<T>((T[])GetSByteArray());

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
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConvertError(ex);
            }
            return lst;
        }
    }
}
