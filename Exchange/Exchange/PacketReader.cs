using System;
using System.Collections.Generic;
using System.Dynamic;
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

        internal object GetValue(Type type, int level)
        {
            if (level > Cache.Limits)
                throw new PacketException(PacketError.RecursiveError);
            level += 1;

            var inf = default(Info);
            if (Cache.TryGetConverter(converters, type, out var con, ref inf))
                return con.GetValueWrap(element, true);
            return GetValueMatch(type, level, inf);
        }

        internal object GetValueMatch(Type type, int level, Info info)
        {
            if (level > Cache.Limits)
                throw new PacketException(PacketError.RecursiveError);
            level += 1;

            switch (info.To)
            {
                case Info.Reader:
                    return this;
                case Info.RawReader:
                    return new PacketRawReader(this);

                case Info.Collection:
                    {
                        var sub = default(Info);
                        if (Cache.TryGetConverter(converters, info.ElementType, out var con, ref sub))
                            return info.ToCollection(this, con);
                        var lst = GetArray();
                        var arr = new object[lst.Length];
                        for (int i = 0; i < lst.Length; i++)
                            arr[i] = lst[i].GetValueMatch(info.ElementType, level, sub);
                        var res = info.ToCollectionCast(arr);
                        return res;
                    }
                case Info.Enumerable:
                    {
                        var sub = default(Info);
                        if (Cache.TryGetConverter(converters, info.ElementType, out var con, ref sub))
                            return info.ToEnumerable(this, con);
                        return info.ToEnumerableAdapter(this, level, sub);
                    }
                case Info.Dictionary:
                    {
                        var sub = default(Info);
                        var keycon = Cache.GetConverter(converters, info.IndexType, true);
                        if (keycon == null)
                            throw PacketException.InvalidKeyType(type);
                        if (Cache.TryGetConverter(converters, info.ElementType, out var con, ref sub))
                            return info.ToDictionary(this, keycon, con);

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
                                    goto fail;
                                else
                                    len = keylen;
                            else if (buf.MoveNext(max, ref idx, out len) == false)
                                goto fail;
                            // Wrap error non-check
                            var key = keycon.GetValueWrap(buf, idx, len);
                            idx += len;

                            if (buf.MoveNext(max, ref idx, out len) == false)
                                goto fail;
                            var rea = new PacketReader(buf, idx, len, converters);
                            var val = rea.GetValueMatch(info.ElementType, level, sub);
                            var par = new KeyValuePair<object, object>(key, val);

                            idx += len;
                            lst.Add(par);
                        }
                        return info.ToDictionaryCast(lst);
                        fail:
                        throw PacketException.Overflow();
                    }
                default:
                    {
                        var set = Cache.GetSetInfo(type);
                        if (set == null)
                            throw PacketException.InvalidType(type);
                        var arg = set.Arguments;
                        var arr = new object[arg.Length];
                        for (int i = 0; i < arg.Length; i++)
                        {
                            var rea = GetItem(arg[i].Key, false);
                            var val = rea.GetValue(arg[i].Value, level);
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
