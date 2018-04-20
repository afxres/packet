using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    public sealed class PacketReader : IDynamicMetaObjectProvider
    {
        private const int TagArray = 1;
        private const int TagDictionary = 2;

        internal readonly ConverterDictionary converters;
        internal readonly Element element;

        private PacketReader[] array = null;
        private Dictionary<string, PacketReader> dictionary = null;
        private int tag = 0;

        public PacketReader(byte[] buffer, ConverterDictionary converters = null)
        {
            element = new Element(buffer);
            this.converters = converters;
        }

        public PacketReader(byte[] buffer, int offset, int length, ConverterDictionary converters = null)
        {
            element = new Element(buffer, offset, length);
            this.converters = converters;
        }

        internal Dictionary<string, PacketReader> GetDictionary()
        {
            var obj = dictionary;
            if (obj != null)
                return obj;
            if ((tag & TagDictionary) != 0)
                return null;
            tag |= TagDictionary;

            var dic = new Dictionary<string, PacketReader>();
            var buf = element.buffer;
            var max = element.Max();
            var idx = element.offset;
            var len = 0;

            try
            {
                while (idx < max)
                {
                    if (buf.MoveNext(max, ref idx, out len) == false)
                        return null;
                    var key = Extension.s_encoding.GetString(buf, idx, len);
                    idx += len;
                    if (buf.MoveNext(max, ref idx, out len) == false)
                        return null;
                    dic.Add(key, new PacketReader(buf, idx, len, converters));
                    idx += len;
                }
            }
            catch (ArgumentException)
            {
                // duplicate key
                dictionary = null;
                return null;
            }

            dictionary = dic;
            return dic;
        }

        internal PacketReader[] GetArray()
        {
            var arr = array;
            if (arr != null)
                return arr;
            if ((tag & TagArray) != 0)
                throw PacketException.Overflow();
            tag |= TagArray;

            var lst = new List<PacketReader>();
            var max = element.Max();
            var idx = element.offset;
            var buf = element.buffer;
            var len = 0;
            while (idx != max)
            {
                if (buf.MoveNext(max, ref idx, out len) == false)
                    throw PacketException.Overflow();
                var rea = new PacketReader(buf, idx, len, converters);
                lst.Add(rea);
                idx += len;
            }
            arr = lst.ToArray();
            array = arr;
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
            return System.Linq.Enumerable.Empty<string>();
        }

        internal object GetValue(Type typ, int lev)
        {
            if (lev > Cache.Depth)
                throw new PacketException(PacketError.RecursiveError);
            lev += 1;

            var inf = default(Info);
            if (Cache.TryGetConverter(converters, typ, out var con, ref inf))
                return con.GetValueWrap(element, true);
            return GetValueMatch(typ, lev, inf);
        }

        internal object GetValueMatch(Type typ, int lev, Info inf)
        {
            if (lev > Cache.Depth)
                throw new PacketException(PacketError.RecursiveError);
            lev += 1;

            var sub = default(Info);
            switch (inf.To)
            {
                case Info.Reader:
                    return this;
                case Info.RawReader:
                    return new PacketRawReader(this);

                case Info.Collection:
                    {
                        if (Cache.TryGetConverter(converters, inf.ElementType, out var con, ref sub))
                            return inf.ToCollection(this, con);
                        var lst = GetArray();
                        var arr = new object[lst.Length];
                        for (int i = 0; i < lst.Length; i++)
                            arr[i] = lst[i].GetValueMatch(inf.ElementType, lev, sub);
                        var res = inf.ToCollectionCast(arr);
                        return res;
                    }
                case Info.Enumerable:
                    {
                        if (Cache.TryGetConverter(converters, inf.ElementType, out var con, ref sub))
                            return inf.ToEnumerable(this, con);
                        return inf.ToEnumerableAdapter(this, lev, sub);
                    }
                case Info.Dictionary:
                    {
                        var keycon = Cache.GetConverter(converters, inf.IndexType, true);
                        if (keycon == null)
                            throw PacketException.InvalidKeyType(typ);
                        if (Cache.TryGetConverter(converters, inf.ElementType, out var con, ref sub))
                            return inf.ToDictionary(this, keycon, con);

                        var max = element.Max();
                        var idx = element.offset;
                        var buf = element.buffer;
                        var keylen = keycon.Length;
                        var len = 0;

                        var lst = new List<KeyValuePair<object, object>>();
                        while (true)
                        {
                            var res = max - idx;
                            if (res == 0)
                                break;
                            if (keylen > 0)
                                if (res < keylen)
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
                            var rea = new PacketReader(buf, idx, len, converters);
                            var val = rea.GetValueMatch(inf.ElementType, lev, sub);
                            var par = new KeyValuePair<object, object>(key, val);

                            idx += len;
                            lst.Add(par);
                        }
                        return inf.ToDictionaryCast(lst);
                    }
                default:
                    {
                        var set = Cache.GetSetterInfo(typ);
                        if (set == null)
                            throw PacketException.InvalidType(typ);
                        var arg = set.Arguments;
                        var arr = new object[arg.Length];
                        for (int i = 0; i < arg.Length; i++)
                        {
                            var rea = GetItem(arg[i].Key, false);
                            var val = rea.GetValue(arg[i].Value, lev);
                            arr[i] = val;
                        }

                        var res = set.GetObject(arr);
                        return res;
                    }
            }
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new DynamicReader(parameter, this);

        public int Count => GetDictionary()?.Count ?? 0;

        public IEnumerable<string> Keys => GetKeys();

        public PacketReader this[string path, bool nothrow = false]
        {
            get
            {
                if (path == null)
                    throw new ArgumentNullException(nameof(path));
                var key = path.Split(Extension.s_separators);
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
            stb.AppendFormat("{0} byte(s)", element.length);
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
