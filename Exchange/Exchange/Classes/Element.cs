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

        internal int Max() => offset + length;

        internal void MoveNext(int def, ref int idx, out int len)
        {
            var max = offset + length;
            if ((def > 0 && idx + def > max) || (def < 1 && buffer.MoveNext(max, ref idx, out def) == false))
                throw PacketException.Overflow();
            len = def;
        }

        internal object Next(ref int idx, IPacketConverter con)
        {
            var tmp = idx;
            MoveNext(con.Length, ref tmp, out var len);
            var res = con.GetValueWrap(buffer, tmp, len);
            idx = tmp + len;
            return res;
        }

        internal T NextAuto<T>(ref int idx, IPacketConverter con)
        {
            var tmp = idx;
            MoveNext(con.Length, ref tmp, out var len);
            var res = con.GetValueWrapAuto<T>(buffer, tmp, len);
            idx = tmp + len;
            return res;
        }

        internal Dictionary<TK, TV> ToDictionary<TK, TV>(IPacketConverter keycon, IPacketConverter valcon)
        {
            var dic = new Dictionary<TK, TV>();
            if (length == 0)
                return dic;
            var keygen = keycon as IPacketConverter<TK>;
            var valgen = valcon as IPacketConverter<TV>;
            var keylen = keycon.Length;
            var vallen = valcon.Length;
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
                            throw PacketException.Overflow();
                        else
                            len = keylen;
                    else if (buffer.MoveNext(max, ref idx, out len) == false)
                        throw PacketException.Overflow();

                    var key = (keygen != null ? keygen.GetValue(buffer, idx, len) : (TK)keycon.GetValue(buffer, idx, len));
                    idx += len;
                    sub = max - idx;

                    if (vallen > 0)
                        if (sub < vallen)
                            throw PacketException.Overflow();
                        else
                            len = vallen;
                    else if (buffer.MoveNext(max, ref idx, out len) == false)
                        throw PacketException.Overflow();

                    var val = (valgen != null ? valgen.GetValue(buffer, idx, len) : (TV)valcon.GetValue(buffer, idx, len));
                    idx += len;
                    dic.Add(key, val);
                }
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConvertError(ex);
            }

            return dic;
        }

        internal T[] ToArray<T>(IPacketConverter con)
        {
            if (length < 1)
                return new T[0];
            if (typeof(T) == typeof(byte))
                return (T[])(object)ByteArrayConverter.ToByteArray(buffer, offset, length);
            else if (typeof(T) == typeof(sbyte))
                return (T[])(object)SByteArrayConverter.ToSbyteArray(buffer, offset, length);
            var def = con.Length;
            var sum = Math.DivRem(length, def, out var rem);
            if (rem != 0)
                throw PacketException.Overflow();
            var arr = new T[sum];
            var gen = con as IPacketConverter<T>;

            try
            {
                if (gen != null)
                    for (int idx = 0; idx < sum; idx++)
                        arr[idx] = gen.GetValue(buffer, offset + idx * def, def);
                else
                    for (int idx = 0; idx < sum; idx++)
                        arr[idx] = (T)con.GetValue(buffer, offset + idx * def, def);
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConvertError(ex);
            }
            return arr;
        }
    }
}
