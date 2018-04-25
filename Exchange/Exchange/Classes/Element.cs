using Mikodev.Network.Converters;
using System;

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

        internal int Max() => offset + length;

        internal void MoveNext(int define, ref int index, out int length)
        {
            var max = offset + this.length;
            if ((define > 0 && index + define > max) || (define < 1 && buffer.MoveNext(max, ref index, out define) == false))
                throw PacketException.Overflow();
            length = define;
        }

        internal object Next(ref int index, IPacketConverter converter)
        {
            var tmp = index;
            MoveNext(converter.Length, ref tmp, out var len);
            var res = converter.GetValueWrap(buffer, tmp, len);
            index = tmp + len;
            return res;
        }

        internal T NextAuto<T>(ref int index, IPacketConverter converter)
        {
            var tmp = index;
            MoveNext(converter.Length, ref tmp, out var len);
            var res = converter.GetValueWrapAuto<T>(buffer, tmp, len);
            index = tmp + len;
            return res;
        }

        internal void ToDictionary<TK, TV>(IPacketConverter indexConverter, IPacketConverter elementConverter, DictionaryAbstract<TK, TV> dictionary)
        {
            if (length == 0)
                return;
            var keygen = indexConverter as IPacketConverter<TK>;
            var valgen = elementConverter as IPacketConverter<TV>;
            var keylen = indexConverter.Length;
            var vallen = elementConverter.Length;
            var max = offset + length;
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

                    var key = (keygen != null ? keygen.GetValue(buffer, idx, len) : (TK)indexConverter.GetValue(buffer, idx, len));
                    idx += len;
                    sub = max - idx;

                    if (vallen > 0)
                        if (sub < vallen)
                            goto fail;
                        else
                            len = vallen;
                    else if (buffer.MoveNext(max, ref idx, out len) == false)
                        goto fail;

                    var val = (valgen != null ? valgen.GetValue(buffer, idx, len) : (TV)elementConverter.GetValue(buffer, idx, len));
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

        internal T[] ToArray<T>(IPacketConverter converter)
        {
            if (length < 1)
                return new T[0];
            if (typeof(T) == typeof(byte))
                return (T[])(object)ByteArrayConverter.ToByteArray(buffer, offset, length);
            else if (typeof(T) == typeof(sbyte))
                return (T[])(object)SByteArrayConverter.ToSbyteArray(buffer, offset, length);
            var def = converter.Length;
            var sum = Math.DivRem(length, def, out var rem);
            if (rem != 0)
                throw PacketException.Overflow();
            var arr = new T[sum];
            var gen = converter as IPacketConverter<T>;

            try
            {
                if (gen != null)
                    for (int idx = 0; idx < sum; idx++)
                        arr[idx] = gen.GetValue(buffer, offset + idx * def, def);
                else
                    for (int idx = 0; idx < sum; idx++)
                        arr[idx] = (T)converter.GetValue(buffer, offset + idx * def, def);
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConvertError(ex);
            }
            return arr;
        }
    }
}
