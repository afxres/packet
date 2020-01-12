using Mikodev.Network.Internal;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
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

        internal readonly Block block;

        private List<PacketReader> list;

        private Dictionary<string, PacketReader> dictionary;

        private Flags flags;

        internal PacketReader(Block block, ConverterDictionary converters)
        {
            this.block = block;
            this.converters = converters;
        }

        public PacketReader(byte[] buffer, ConverterDictionary converters = null)
        {
            this.block = new Block(buffer);
            this.converters = converters;
        }

        public PacketReader(byte[] buffer, int offset, int length, ConverterDictionary converters = null)
        {
            this.block = new Block(buffer, offset, length);
            this.converters = converters;
        }

        internal Dictionary<string, PacketReader> GetDictionary() => this.dictionary ?? this.InitializeDictionary();

        internal List<PacketReader> GetList() => this.list ?? this.InitializeList();

        private Dictionary<string, PacketReader> InitializeDictionary()
        {
            if ((this.flags & Flags.Dictionary) != 0)
                return null;
            this.flags |= Flags.Dictionary;

            var collection = new Dictionary<string, PacketReader>(Extension.Capacity);
            var vernier = (Vernier)this.block;
            try
            {
                while (vernier.Any)
                {
                    if (!vernier.TryFlush())
                        return null;
                    var key = PacketConvert.Encoding.GetString(vernier.Buffer, vernier.Offset, vernier.Length);
                    if (!vernier.TryFlush())
                        return null;
                    collection.Add(key, new PacketReader((Block)vernier, this.converters));
                }
            }
            catch (ArgumentException)
            {
                return null;
            }
            this.dictionary = collection;
            return collection;
        }

        private List<PacketReader> InitializeList()
        {
            if ((this.flags & Flags.List) != 0)
                throw PacketException.Overflow();
            this.flags |= Flags.List;

            var collection = new List<PacketReader>();
            var vernier = (Vernier)this.block;
            while (vernier.Any)
            {
                vernier.Flush();
                collection.Add(new PacketReader((Block)vernier, this.converters));
            }
            this.list = collection;
            return collection;
        }

        /// <summary>
        /// <paramref name="key"/> not null
        /// </summary>
        internal PacketReader GetReader(string key, bool nothrow)
        {
            var dic = this.GetDictionary();
            if (dic != null && dic.TryGetValue(key, out var val))
                return val;
            if (nothrow)
                return null;
            throw new PacketException(dic == null ? PacketError.Overflow : PacketError.InvalidPath);
        }

        /// <summary>
        /// <paramref name="keys"/> not null
        /// </summary>
        internal PacketReader GetReader(IEnumerable<string> keys, bool nothrow)
        {
            var rdr = this;
            foreach (var i in keys)
                if ((rdr = rdr.GetReader(i ?? throw new ArgumentNullException(), nothrow)) == null)
                    return null;
            return rdr;
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new DynamicReader(parameter, this);

        public int Count => this.GetDictionary()?.Count ?? 0;

        public IEnumerable<string> Keys => this.GetDictionary()?.Keys ?? System.Linq.Enumerable.Empty<string>();

        public PacketReader this[string path, bool nothrow = false]
        {
            get
            {
                if (path == null)
                    throw new ArgumentNullException(nameof(path));
                var key = path.Split(Extension.Separator);
                var val = this.GetReader(key, nothrow);
                return val;
            }
        }

        public override string ToString() => $"{nameof(PacketReader)}(Nodes: {this.GetDictionary()?.Count ?? 0}, Bytes: {this.block.Length})";

        public T Deserialize<T>() => (T)this.GetValue(typeof(T), 0);

        public T Deserialize<T>(T anonymous) => (T)this.GetValue(typeof(T), 0);

        public object Deserialize(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            return this.GetValue(type, 0);
        }
    }
}
