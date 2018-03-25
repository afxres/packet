using Mikodev.Network.Converters;
using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal struct _Element
    {
        internal readonly byte[] _buf;
        internal readonly int _off;
        internal readonly int _len;

        internal _Element(byte[] buffer)
        {
            _buf = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _off = 0;
            _len = buffer.Length;
        }

        internal _Element(byte[] buffer, int offset, int length)
        {
            _buf = buffer ?? throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || length < 0 || buffer.Length - offset < length)
                throw new ArgumentOutOfRangeException();
            _off = offset;
            _len = length;
        }

        internal int Max() => _off + _len;

        internal void MoveNext(int def, ref int idx, out int len)
        {
            var max = _off + _len;
            if ((def > 0 && idx + def > max) || (def < 1 && _buf.MoveNext(max, ref idx, out def) == false))
                throw PacketException.Overflow();
            len = def;
        }

        internal object Next(ref int idx, IPacketConverter con)
        {
            var tmp = idx;
            MoveNext(con.Length, ref tmp, out var len);
            var res = con.GetValueWrap(_buf, tmp, len);
            idx = tmp + len;
            return res;
        }

        internal T NextAuto<T>(ref int idx, IPacketConverter con)
        {
            var tmp = idx;
            MoveNext(con.Length, ref tmp, out var len);
            var res = con.GetValueWrapAuto<T>(_buf, tmp, len);
            idx = tmp + len;
            return res;
        }

        internal Dictionary<TK, TV> ToDictionary<TK, TV>(IPacketConverter keycon, IPacketConverter valcon)
        {
            var dic = new Dictionary<TK, TV>();
            if (_len == 0)
                return dic;
            var keygen = keycon as IPacketConverter<TK>;
            var valgen = valcon as IPacketConverter<TV>;
            var keylen = keycon.Length;
            var vallen = valcon.Length;
            var max = _off + _len;
            var idx = _off;
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
                    else if (_buf.MoveNext(max, ref idx, out len) == false)
                        throw PacketException.Overflow();

                    var key = (keygen != null ? keygen.GetValue(_buf, idx, len) : (TK)keycon.GetValue(_buf, idx, len));
                    idx += len;
                    sub = max - idx;

                    if (vallen > 0)
                        if (sub < vallen)
                            throw PacketException.Overflow();
                        else
                            len = vallen;
                    else if (_buf.MoveNext(max, ref idx, out len) == false)
                        throw PacketException.Overflow();

                    var val = (valgen != null ? valgen.GetValue(_buf, idx, len) : (TV)valcon.GetValue(_buf, idx, len));
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
            if (_len < 1)
                return new T[0];
            if (typeof(T) == typeof(byte))
                return (T[])(object)ByteArrayConverter.ToByteArray(_buf, _off, _len);
            else if (typeof(T) == typeof(sbyte))
                return (T[])(object)SByteArrayConverter.ToSbyteArray(_buf, _off, _len);
            var def = con.Length;
            var sum = Math.DivRem(_len, def, out var rem);
            if (rem != 0)
                throw PacketException.Overflow();
            var arr = new T[sum];
            var gen = con as IPacketConverter<T>;

            try
            {
                if (gen != null)
                    for (int idx = 0; idx < sum; idx++)
                        arr[idx] = gen.GetValue(_buf, _off + idx * def, def);
                else
                    for (int idx = 0; idx < sum; idx++)
                        arr[idx] = (T)con.GetValue(_buf, _off + idx * def, def);
            }
            catch (Exception ex) when (PacketException.WrapFilter(ex))
            {
                throw PacketException.ConvertError(ex);
            }
            return arr;
        }
    }
}
