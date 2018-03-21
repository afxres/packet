using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;
using PacketReaderDictionary = System.Collections.Generic.Dictionary<string, Mikodev.Network.PacketReader>;

namespace Mikodev.Network
{
    public sealed class PacketReader : IDynamicMetaObjectProvider
    {
        private const int Init = 1;
        private const int InitArray = 2;

        internal readonly ConverterDictionary _cvt;
        internal readonly _Element _ele;

        private PacketReader[] _arr = null;
        private PacketReaderDictionary _dic = null;
        private int _tag = 0;

        private PacketReader(_Element ele, ConverterDictionary cvt)
        {
            _ele = ele;
            _cvt = cvt;
        }

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

        internal PacketReaderDictionary GetDictionary()
        {
            var obj = _dic;
            if (obj != null)
                return obj;
            if ((_tag & Init) != 0)
                return null;
            _tag |= Init;

            var dic = new PacketReaderDictionary();
            var buf = _ele._buf;
            var max = _ele.Max();
            var idx = _ele._off;
            var len = 0;

            while (idx < max)
            {
                if (buf.MoveNext(max, ref idx, out len) == false)
                    return null;
                var key = _Extension.s_encoding.GetString(buf, idx, len);
                if (dic.ContainsKey(key))
                    return null;
                idx += len;
                if (buf.MoveNext(max, ref idx, out len) == false)
                    return null;
                dic.Add(key, new PacketReader(buf, idx, len, _cvt));
                idx += len;
            }

            _dic = dic;
            return dic;
        }

        internal PacketReader[] GetArray()
        {
            var arr = _arr;
            if (arr != null)
                return arr;
            if ((_tag & InitArray) != 0)
                throw PacketException.Overflow();
            _tag |= InitArray;

            var lst = _ele.GetElementList();
            var len = lst.Count;
            arr = new PacketReader[len];
            for (int i = 0; i < len; i++)
            {
                arr[i] = new PacketReader(lst[i], _cvt);
            }
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

        public int Count => GetDictionary()?.Count ?? 0;

        public IEnumerable<string> Keys => GetDictionary()?.Keys ?? Enumerable.Empty<string>();

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

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new _DynamicReader(parameter, this);

        internal object GetValue(Type typ, int lev)
        {
            if (lev > _Caches.Depth)
                throw new PacketException(PacketError.RecursiveError);
            lev += 1;

            if (typ == typeof(object) || typ == typeof(PacketReader))
                return this;
            if (typ == typeof(PacketRawReader))
                return new PacketRawReader(this);

            var con = _Caches.GetConverter(_cvt, typ, true);
            if (con != null)
                return con.GetValueWrap(_ele, true);

            var inf = _Caches.GetInfo(typ);
            var tag = inf.Flags;
            var ele = inf.ElementType;
            if (ele != null)
                con = _Caches.GetConverter(_cvt, ele, true);

            if ((tag & _Inf.Array) != 0)
            {
                if (con != null)
                    return inf.GetArray(this, con);
                var val = GetValueArray(ele, lev);
                var res = inf.CastToArray(val);
                return res;
            }
            if ((tag & _Inf.List) != 0)
            {
                if (con != null)
                    return inf.GetList(this, con);
                var val = GetValueArray(ele, lev);
                var res = inf.CastToList(val);
                return res;
            }
            if ((tag & _Inf.Enumerable) != 0)
            {
                if (con != null)
                    return inf.GetEnumerable(this, con);
                return inf.GetEnumerableReader(this, lev);
            }
            else if ((tag & _Inf.Collection) != 0)
            {
                if (con != null)
                    return inf.GetCollection(this, con);
                var val = GetValueArray(ele, lev);
                var res = inf.CastToCollection(val);
                return res;
            }
            else if ((tag & _Inf.Dictionary) != 0)
            {
                var keycon = _Caches.GetConverter(_cvt, inf.IndexType, true);
                if (keycon == null)
                    throw new PacketException(PacketError.InvalidKeyType);
                if (con != null)
                    return inf.GetDictionary(this, keycon, con);

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
                return inf.CastToDictionary(lst);
            }
            else
            {
                var set = _Caches.GetSetterInfo(typ);
                var arg = set.Arguments;
                var fun = set.Function;
                if (arg == null || fun == null)
                    throw new PacketException(PacketError.InvalidType);

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
