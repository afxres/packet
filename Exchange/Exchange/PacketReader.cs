using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using ConverterDictionary = System.Collections.Generic.Dictionary<System.Type, Mikodev.Network.PacketConverter>;

namespace Mikodev.Network
{
    public sealed partial class PacketReader : IDynamicMetaObjectProvider
    {
        private enum Flags : int
        {
            None = 0,
            List = 1,
            Dictionary = 2,
        }

        internal readonly ConverterDictionary converters;
        internal readonly Element element;

        private List<PacketReader> list = null;
        private Dictionary<string, PacketReader> dictionary = null;
        private Flags flags = 0;

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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Dictionary<string, PacketReader> GetDictionary() => dictionary ?? InitializeDictionary();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal List<PacketReader> GetList() => list ?? InitializeList();

        private Dictionary<string, PacketReader> InitializeDictionary()
        {
            if ((flags & Flags.Dictionary) != 0)
                return null;
            flags |= Flags.Dictionary;

            var collection = new Dictionary<string, PacketReader>();
            var buffer = element.buffer;
            var limits = element.Limits;
            var offset = element.offset;
            var length = 0;

            try
            {
                while (offset != limits)
                {
                    if ((length = buffer.MoveNext(ref offset, limits)) < 0)
                        return null;
                    var key = Extension.Encoding.GetString(buffer, offset, length);
                    offset += length;
                    if ((length = buffer.MoveNext(ref offset, limits)) < 0)
                        return null;
                    collection.Add(key, new PacketReader(buffer, offset, length, converters));
                    offset += length;
                }
            }
            catch (ArgumentException)
            {
                // duplicate key
                dictionary = null;
                return null;
            }

            dictionary = collection;
            return collection;
        }

        private List<PacketReader> InitializeList()
        {
            if ((flags & Flags.List) != 0)
                throw PacketException.Overflow();
            flags |= Flags.List;

            var lst = new List<PacketReader>();
            var limits = element.Limits;
            var offset = element.offset;
            var buffer = element.buffer;
            var length = 0;
            while (offset != limits)
            {
                length = buffer.MoveNextExcept(ref offset, limits, 0);
                var rea = new PacketReader(buffer, offset, length, converters);
                lst.Add(rea);
                offset += length;
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

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new DynamicReader(parameter, this);

        public int Count => GetDictionary()?.Count ?? 0;

        public IEnumerable<string> Keys => GetDictionary()?.Keys ?? System.Linq.Enumerable.Empty<string>();

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
            var builder = new StringBuilder(nameof(PacketReader));
            builder.Append(" with ");
            var collection = GetDictionary();
            if (collection != null)
                builder.AppendFormat("{0} node(s), ", collection.Count);
            builder.AppendFormat("{0} byte(s)", element.length);
            return builder.ToString();
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
