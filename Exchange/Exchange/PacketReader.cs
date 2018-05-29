using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using System.Text;
using ConverterDictionary = System.Collections.Generic.Dictionary<System.Type, Mikodev.Network.PacketConverter>;

namespace Mikodev.Network
{
    public sealed partial class PacketReader : IDynamicMetaObjectProvider
    {
        private const int TagArray = 1;
        private const int TagDictionary = 2;

        internal readonly ConverterDictionary converters;
        internal readonly Element element;

        private List<PacketReader> list = null;
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
            var max = element.Limits;
            var idx = element.offset;
            var len = 0;

            try
            {
                while (idx != max)
                {
                    if ((len = buf.MoveNext(ref idx, max)) < 0)
                        return null;
                    var key = Extension.Encoding.GetString(buf, idx, len);
                    idx += len;
                    if ((len = buf.MoveNext(ref idx, max)) < 0)
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

        internal List<PacketReader> GetList()
        {
            var src = list;
            if (src != null)
                return src;
            if ((tag & TagArray) != 0)
                throw PacketException.Overflow();
            tag |= TagArray;

            var lst = new List<PacketReader>();
            var max = element.Limits;
            var idx = element.offset;
            var buf = element.buffer;
            var len = 0;
            while (idx != max)
            {
                len = buf.MoveNextExcept(ref idx, max, 0);
                var rea = new PacketReader(buf, idx, len, converters);
                lst.Add(rea);
                idx += len;
            }
            list = lst;
            return lst;
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
            throw new PacketException(dic == null ? PacketError.Overflow : PacketError.InvalidPath);
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

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new DynamicReader(parameter, this);

        public int Count => GetDictionary()?.Count ?? 0;

        public IEnumerable<string> Keys => GetKeys();

        public PacketReader this[string path, bool nothrow = false]
        {
            get
            {
                if (path == null)
                    throw new ArgumentNullException(nameof(path));
                var key = path.Split(Extension.Separator);
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

        public T Deserialize<T>() => (T)GetValue(typeof(T), 0);

        public T Deserialize<T>(T anonymous) => (T)GetValue(typeof(T), 0);

        public object Deserialize(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            return GetValue(type, 0);
        }
    }
}
