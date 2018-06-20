using System;
using System.Collections.Generic;

namespace Mikodev.Binary
{
    public sealed class Token
    {
        private static readonly Dictionary<string, Token> empty = new Dictionary<string, Token>();

        private readonly Cache cache;
        private readonly Block block;
        private Dictionary<string, Token> dictionary;

        internal Token(Cache cache, Block block)
        {
            this.cache = cache;
            this.block = block;
        }

        private Dictionary<string, Token> GetDictionary()
        {
            var map = dictionary;
            if (map != null)
                return map;
            try
            {
                var vernier = new Vernier(block);
                while (vernier.Any)
                {
                    if (!vernier.TryFlush())
                        goto fail;
                    var key = Extension.Encoding.GetString(vernier.Buffer, vernier.Offset, vernier.Length);
                    if (!vernier.TryFlush())
                        goto fail;
                    var value = new Token(cache, (Block)vernier);
                    if (map == null)
                        map = new Dictionary<string, Token>(8);
                    map.Add(key, value);
                }
                dictionary = map;
                return map;
            }
            catch (Exception) { }

            fail:
            dictionary = empty;
            return empty;
        }

        public Token this[string key] => GetDictionary()[key];

        public T As<T>()
        {
            var converter = (Converter<T>)cache.GetOrCreateConverter(typeof(T));
            var value = converter.ToValue(block);
            return value;
        }

        public T As<T>(T anonymous) => As<T>();

        public object As(Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));
            var converter = cache.GetOrCreateConverter(type);
            var value = converter.ToValueNonGeneric(block);
            return value;
        }
    }
}
