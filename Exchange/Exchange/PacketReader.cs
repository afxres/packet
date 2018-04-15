using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using static Mikodev.Network._Extension;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    public sealed class PacketReader : IDynamicMetaObjectProvider
    {
        private const int TagArray = 1;
        private const int TagDictionary = 2;

        internal readonly ConverterDictionary _cvt;
        internal readonly _Element _ele;

        private PacketReader[] _arr = null;
        private Dictionary<string, PacketReader> _dic = null;
        private int _tag = 0;

        public PacketReader(byte[] buffer, ConverterDictionary converters = null)
        {
            _ele = new _Element(buffer);
            _cvt = converters;
        }

        public PacketReader(byte[] buffer, int offset, int length, ConverterDictionary converters = null)
        {
            _ele = new _Element(buffer, offset, length);
            _cvt = converters;
        }

        internal Dictionary<string, PacketReader> GetDictionary()
        {
            var obj = _dic;
            if (obj != null)
                return obj;
            if ((_tag & TagDictionary) != 0)
                return null;
            _tag |= TagDictionary;

            var dic = new Dictionary<string, PacketReader>();
            var buf = _ele._buf;
            var max = _ele.Max();
            var idx = _ele._off;
            var len = 0;

            try
            {
                while (idx < max)
                {
                    if (buf.MoveNext(max, ref idx, out len) == false)
                        return null;
                    var key = _Extension.s_encoding.GetString(buf, idx, len);
                    idx += len;
                    if (buf.MoveNext(max, ref idx, out len) == false)
                        return null;
                    dic.Add(key, new PacketReader(buf, idx, len, _cvt));
                    idx += len;
                }
            }
            catch (ArgumentException)
            {
                // duplicate key
                _dic = null;
                return null;
            }

            _dic = dic;
            return dic;
        }

        internal PacketReader[] GetArray()
        {
            var arr = _arr;
            if (arr != null)
                return arr;
            if ((_tag & TagArray) != 0)
                throw PacketException.Overflow();
            _tag |= TagArray;

            var lst = new List<PacketReader>();
            var max = _ele.Max();
            var idx = _ele._off;
            var buf = _ele._buf;
            var len = 0;
            while (idx != max)
            {
                if (buf.MoveNext(max, ref idx, out len) == false)
                    throw PacketException.Overflow();
                var rea = new PacketReader(buf, idx, len, _cvt);
                lst.Add(rea);
                idx += len;
            }
            arr = lst.ToArray();
            _arr = arr;
            return arr;
        }

        /// <summary>
        /// <paramref name="key"/> not null
        /// </summary>
        internal PacketReader GetItem(string key, bool nothrow)
        {
            var dic = GetDictionary();
            if (dic != null && dic.TryGetValue(key, out var val))
                return val;
            if (nothrow)
                return null;
            throw new PacketException(dic == null ? PacketError.Overflow : PacketError.PathError);
        }

        /// <summary>
        /// <paramref name="keys"/> not null
        /// </summary>
        internal PacketReader GetItem(IEnumerable<string> keys, bool nothrow)
        {
            var rdr = this;
            foreach (var i in keys)
                if ((rdr = rdr.GetItem(i ?? throw new ArgumentNullException(), nothrow)) == null)
                    return null;
            return rdr;
        }

        internal IEnumerable<string> GetKeys()
        {
            var dic = GetDictionary();
            if (dic != null)
                return dic.Keys;
            return Enumerable.Empty<string>();
        }

        internal object GetValue(Type typ, int lev)
        {
            if (lev > _Caches.Depth)
                throw new PacketException(PacketError.RecursiveError);
            lev += 1;

            var inf = default(_Inf);
            if (_Caches.TryGetConverter(_cvt, typ, out var con) || ((inf = _Caches.GetInfo(typ)).Flag == _Inf.Enum && s_converters.TryGetValue(inf.ElementType, out con)))
                return con.GetValueWrap(_ele, true);
            return GetValueMatch(typ, lev, inf);
        }

        private object GetValueMatch(Type typ, int lev, _Inf inf)
        {
            if (lev > _Caches.Depth)
                throw new PacketException(PacketError.RecursiveError);
            lev += 1;

            var ele = inf.ElementType;
            var con = (ele != null ? _Caches.GetConverter(_cvt, ele, true) : null);
            switch (inf.To)
            {
                case _Inf.Reader:
                    return this;
                case _Inf.RawReader:
                    return new PacketRawReader(this);

                case _Inf.Array:
                    {
                        if (con != null)
                            return inf.ToCollection(this, con);
                        var val = GetValueArray(ele, lev);
                        var res = inf.ToCollectionCast(val);
                        return res;
                    }
                case _Inf.List:
                    {
                        if (con != null)
                            return inf.ToCollection(this, con);
                        var val = GetValueArray(ele, lev);
                        var res = inf.ToCollectionCast(val);
                        return res;
                    }
                case _Inf.Enumerable:
                    {
                        if (con != null)
                            return inf.ToEnumerable(this, con);
                        return inf.ToEnumerableAdapter(this, lev);
                    }
                case _Inf.Collection:
                    {
                        if (con != null)
                            return inf.ToCollection(this, con);
                        var val = GetValueArray(ele, lev);
                        var res = inf.ToCollectionCast(val);
                        return res;
                    }
                case _Inf.Dictionary:
                    {
                        var keycon = _Caches.GetConverter(_cvt, inf.IndexType, true);
                        if (keycon == null)
                            throw PacketException.InvalidKeyType(typ);
                        if (con != null)
                            return inf.ToDictionary(this, keycon, con);

                        var max = _ele.Max();
                        var idx = _ele._off;
                        var buf = _ele._buf;
                        var keylen = keycon.Length;
                        var len = 0;

                        var lst = new List<KeyValuePair<object, object>>();
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
                            else if (buf.MoveNext(max, ref idx, out len) == false)
                                throw PacketException.Overflow();
                            // Wrap error non-check
                            var key = keycon.GetValueWrap(buf, idx, len);
                            idx += len;

                            if (buf.MoveNext(max, ref idx, out len) == false)
                                throw PacketException.Overflow();
                            var rea = new PacketReader(buf, idx, len, _cvt);
                            var val = rea.GetValue(ele, lev);
                            var par = new KeyValuePair<object, object>(key, val);

                            idx += len;
                            lst.Add(par);
                        }
                        return inf.ToDictionaryCast(lst);
                    }
                default:
                    {
                        var set = _Caches.GetSetterInfo(typ);
                        var arg = set.Arguments;
                        var fun = set.Function;
                        if (arg == null || fun == null)
                            throw PacketException.InvalidType(typ);

                        var arr = new object[arg.Length];
                        for (int i = 0; i < arg.Length; i++)
                        {
                            var rea = GetItem(arg[i].Name, false);
                            var val = rea.GetValue(arg[i].Type, lev);
                            arr[i] = val;
                        }

                        var res = fun.Invoke(arr);
                        return res;
                    }
            }
        }

        internal object[] GetValueArray(Type ele, int lev)
        {
            if (lev > _Caches.Depth)
                throw new PacketException(PacketError.RecursiveError);
            lev += 1;

            var lst = GetArray();
            var arr = new object[lst.Length];
            for (int i = 0; i < lst.Length; i++)
                arr[i] = lst[i].GetValue(ele, lev);
            return arr;
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new _DynamicReader(parameter, this);

        public int Count => GetDictionary()?.Count ?? 0;

        public IEnumerable<string> Keys => GetKeys();

        public PacketReader this[string path, bool nothrow = false]
        {
            get
            {
                if (path == null)
                    throw new ArgumentNullException(nameof(path));
                var key = path.Split(_Extension.s_separators);
                var val = GetItem(key, nothrow);
                return val;
            }
        }

        public override string ToString()
        {
            var stb = new StringBuilder(nameof(PacketReader));
            stb.Append(" with ");
            var dic = GetDictionary();
            if (dic != null)
                stb.AppendFormat("{0} node(s), ", dic.Count);
            stb.AppendFormat("{0} byte(s)", _ele._len);
            return stb.ToString();
        }

        public T Deserialize<T>(T anonymous) => (T)GetValue(typeof(T), 0);

        public T Deserialize<T>() => (T)GetValue(typeof(T), 0);

        public object Deserialize(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            return GetValue(type, 0);
        }
    }
}
