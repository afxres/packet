using System;
using System.Collections.Generic;
using static Mikodev.Network._Extension;

namespace Mikodev.Network
{
    internal struct _Element
    {
        internal readonly byte[] _buf;
        internal readonly int _off;
        internal readonly int _len;
        internal readonly int _max;
        internal int _idx;

        internal _Element(_Element ele)
        {
            this = ele;
            _idx = ele._off;
        }

        internal _Element(byte[] buffer)
        {
            _buf = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _off = 0;
            _idx = 0;
            _len = buffer.Length;
            _max = buffer.Length;
        }

        internal _Element(byte[] buffer, int offset, int length)
        {
            _buf = buffer ?? throw new ArgumentNullException(nameof(buffer));
            if (offset < 0 || length < 0 || buffer.Length - offset < length)
                throw new ArgumentOutOfRangeException();
            _off = offset;
            _idx = offset;
            _len = length;
            _max = offset + length;
        }

        internal bool End() => _idx >= _max;

        internal bool Any() => _idx < _max;

        internal void _EnsureNext(int def, out int pos, out int len)
        {
            var idx = _idx;
            if ((def > 0 && idx + def > _max) || (def < 1 && _buf._Read(_max, ref idx, out def) == false))
                throw _Overflow();
            pos = idx;
            len = def;
        }

        internal object Next(IPacketConverter con)
        {
            _EnsureNext(con.Length, out var pos, out var len);
            var res = con._GetValueWrapError(_buf, pos, len, false);
            _idx = pos + len;
            return res;
        }

        internal T NextGeneric<T>(IPacketConverter<T> con)
        {
            _EnsureNext(con.Length, out var pos, out var len);
            var res = con._GetValueWrapErrorGeneric(_buf, pos, len, false);
            _idx = pos + len;
            return res;
        }

        internal T NextAuto<T>(IPacketConverter con)
        {
            _EnsureNext(con.Length, out var pos, out var len);
            var res = con._GetValueWrapErrorAuto<T>(_buf, pos, len, false);
            _idx = pos + len;
            return res;
        }

        internal bool _EnsureBuild(ref int idx, out int pos, out int len)
        {
            if (idx == _max)
                goto fail;
            if (_buf._Read(_max, ref idx, out var tmp) == false)
                throw _Overflow();
            pos = idx;
            len = tmp;
            idx = pos + tmp;
            return true;

            fail:
            pos = 0;
            len = 0;
            return false;
        }

        internal object _BuildVariable<T>(IPacketConverter con)
        {
            var idx = _off;
            var lst = new List<T>();
            var gen = con as IPacketConverter<T>;
            var pos = default(int);
            var len = default(int);

            try
            {
                if (gen != null)
                    while (_EnsureBuild(ref idx, out pos, out len))
                        lst.Add(gen.GetValue(_buf, pos, len));
                else
                    while (_EnsureBuild(ref idx, out pos, out len))
                        lst.Add((T)con.GetValue(_buf, pos, len));
            }
            catch (Exception ex) when (_WrapError(ex))
            {
                throw _ConvertError(ex);
            }
            return lst;
        }

        internal object _BuildCollection<T>(IPacketConverter con)
        {
            if (_len < 1)
                return null;

            var len = con.Length;
            if (len < 1)
                return _BuildVariable<T>(con);

            var buf = _buf;
            var off = _off;
            var sum = Math.DivRem(_len, len, out var rem);
            if (rem != 0)
                throw _Overflow();
            var arr = new T[sum];
            var gen = con as IPacketConverter<T>;

            try
            {
                if (gen != null)
                    for (int i = 0; i < sum; i++)
                        arr[i] = gen.GetValue(buf, off + i * len, len);
                else
                    for (int i = 0; i < sum; i++)
                        arr[i] = (T)con.GetValue(buf, off + i * len, len);
            }
            catch (Exception ex) when (_WrapError(ex))
            {
                throw _ConvertError(ex);
            }
            return arr;
        }

        internal List<T> List<T>(IPacketConverter con)
        {
            var res = _BuildCollection<T>(con);
            if (res == null)
                return new List<T>();
            if (res is T[] arr)
                return new List<T>(arr);
            if (res is List<T> lst)
                return lst;
            throw new InvalidOperationException();
        }

        internal T[] Array<T>(IPacketConverter con)
        {
            var res = _BuildCollection<T>(con);
            if (res == null)
                return new T[0];
            if (res is T[] arr)
                return arr;
            if (res is List<T> lst)
                return lst.ToArray();
            throw new InvalidOperationException();
        }

        internal byte[] GetBytes()
        {
            return _buf._ToBytes(_off, _len);
        }
    }
}
