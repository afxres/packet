using Mikodev.Network.Converters;
using System;
using System.Collections.Generic;
using static Mikodev.Network._Extension;

namespace Mikodev.Network
{
    internal struct _Element
    {
        internal readonly byte[] _buffer;
        internal readonly int _offset;
        internal readonly int _length;
        internal int _index;

        internal _Element(_Element ele)
        {
            _buffer = ele._buffer;
            _offset = ele._offset;
            _index = ele._offset;
            _length = ele._length;
        }

        internal _Element(byte[] buffer)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _offset = 0;
            _index = 0;
            _length = buffer.Length;
        }

        internal _Element(byte[] buffer, int offset, int length)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || length < 0 || buffer.Length - offset < length)
                throw new ArgumentOutOfRangeException();
            _offset = offset;
            _index = offset;
            _length = length;
        }

        internal bool End() => _index >= (_offset + _length);

        internal bool Any() => _index < (_offset + _length);

        internal void Reset() => _index = _offset;

        internal void _EnsureNext(int def, out int pos, out int len)
        {
            var idx = _index;
            var max = _offset + _length;
            if ((def > 0 && idx + def > max) || (def < 1 && _buffer._HasNext(max, ref idx, out def) == false))
                throw PacketException.ThrowOverflow();
            pos = idx;
            len = def;
        }

        internal object Next(IPacketConverter con)
        {
            _EnsureNext(con.Length, out var pos, out var len);
            var res = con._GetValueWrapError(_buffer, pos, len, false);
            _index = pos + len;
            return res;
        }

        internal T NextGeneric<T>(IPacketConverter<T> con)
        {
            _EnsureNext(con.Length, out var pos, out var len);
            var res = con._GetValueWrapErrorGeneric(_buffer, pos, len, false);
            _index = pos + len;
            return res;
        }

        internal T NextAuto<T>(IPacketConverter con)
        {
            _EnsureNext(con.Length, out var pos, out var len);
            var res = con._GetValueWrapErrorAuto<T>(_buffer, pos, len, false);
            _index = pos + len;
            return res;
        }

        internal Dictionary<TK, TV> Dictionary<TK, TV>(IPacketConverter keycon, IPacketConverter valcon)
        {
            var dic = new Dictionary<TK, TV>();
            if (_length == 0)
                return dic;
            var keygen = keycon as IPacketConverter<TK>;
            var valgen = valcon as IPacketConverter<TV>;
            var keylen = keycon.Length;
            var vallen = valcon.Length;
            var max = _offset + _length;
            var idx = _offset;
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
                            throw PacketException.ThrowOverflow();
                        else
                            len = keylen;
                    else if (_buffer._HasNext(max, ref idx, out len) == false)
                        throw PacketException.ThrowOverflow();

                    var key = (keygen != null ? keygen.GetValue(_buffer, idx, len) : (TK)keycon.GetValue(_buffer, idx, len));
                    idx += len;
                    sub = max - idx;

                    if (vallen > 0)
                        if (sub < vallen)
                            throw PacketException.ThrowOverflow();
                        else
                            len = vallen;
                    else if (_buffer._HasNext(max, ref idx, out len) == false)
                        throw PacketException.ThrowOverflow();

                    var val = (valgen != null ? valgen.GetValue(_buffer, idx, len) : (TV)valcon.GetValue(_buffer, idx, len));
                    idx += len;
                    dic.Add(key, val);
                }
            }
            catch (Exception ex) when (_WrapError(ex))
            {
                throw PacketException.ThrowConvertError(ex);
            }

            return dic;
        }

        internal IEnumerable<T> _List<T>(IPacketConverter con)
        {
            var lst = new List<T>();
            var gen = con as IPacketConverter<T>;
            var max = _offset + _length;
            var idx = _offset;
            var len = default(int);

            try
            {
                while (idx != max)
                {
                    if (_buffer._HasNext(max, ref idx, out len) == false)
                        throw PacketException.ThrowOverflow();
                    var buf = (gen != null ? gen.GetValue(_buffer, idx, len) : (T)con.GetValue(_buffer, idx, len));
                    lst.Add(buf);
                    idx += len;
                }
            }
            catch (Exception ex) when (_WrapError(ex))
            {
                throw PacketException.ThrowConvertError(ex);
            }
            return lst;
        }

        internal IEnumerable<T> Collection<T>(IPacketConverter con)
        {
            if (_length < 1)
                return new T[0];
            if (typeof(T) == typeof(byte))
                return (IEnumerable<T>)(object)ByteArrayConverter.ToByteArray(_buffer, _offset, _length);
            else if (typeof(T) == typeof(sbyte))
                return (IEnumerable<T>)(object)SByteArrayConverter.ToSbyteArray(_buffer, _offset, _length);

            var len = con.Length;
            if (len < 1)
                return _List<T>(con);

            var sum = Math.DivRem(_length, len, out var rem);
            if (rem != 0)
                throw PacketException.ThrowOverflow();
            var arr = new T[sum];
            var gen = con as IPacketConverter<T>;

            try
            {
                if (gen != null)
                    for (int i = 0; i < sum; i++)
                        arr[i] = gen.GetValue(_buffer, _offset + i * len, len);
                else
                    for (int i = 0; i < sum; i++)
                        arr[i] = (T)con.GetValue(_buffer, _offset + i * len, len);
            }
            catch (Exception ex) when (_WrapError(ex))
            {
                throw PacketException.ThrowConvertError(ex);
            }
            return arr;
        }

        internal List<T> List<T>(IPacketConverter con)
        {
            var res = Collection<T>(con);
            if (res is T[] arr)
                return new List<T>(arr);
            if (res is List<T> lst)
                return lst;
            throw new InvalidOperationException();
        }

        internal T[] Array<T>(IPacketConverter con)
        {
            var res = Collection<T>(con);
            if (res is T[] arr)
                return arr;
            if (res is List<T> lst)
                return lst.ToArray();
            throw new InvalidOperationException();
        }
    }
}
