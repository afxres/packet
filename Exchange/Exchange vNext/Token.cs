using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Linq.Expressions;

namespace Mikodev.Binary
{
    [DebuggerTypeProxy(typeof(TokenDebugProxy))]
    public sealed class Token : IDynamicMetaObjectProvider
    {
        private static readonly Dictionary<string, Token> empty = new Dictionary<string, Token>();

        private readonly Cache cache;
        private readonly ReadOnlyMemory<byte> memory;
        private Dictionary<string, Token> dictionary;

        internal Dictionary<string, Token> Tokens => dictionary ?? GetDictionary();

        internal Token(Cache cache, ReadOnlyMemory<byte> memory)
        {
            this.cache = cache;
            this.memory = memory;
        }

        private Dictionary<string, Token> GetDictionary()
        {
            var collection = default(Dictionary<string, Token>);

            try
            {
                var span = memory.Span;
                ref readonly var location = ref span[0];
                var vernier = new Vernier(memory.Length);
                while (vernier.Any())
                {
                    vernier.Flush(in location);
                    var key = Converter.Encoding.GetString(in span[vernier.offset], vernier.length);
                    vernier.Flush(in location);
                    var value = new Token(cache, memory.Slice(vernier.offset, vernier.length));
                    if (collection == null)
                        collection = new Dictionary<string, Token>(8);
                    collection.Add(key, value);
                }
            }
            catch (Exception)
            {
                collection = null;
            }

            if (collection == null)
                collection = empty;
            dictionary = collection;
            return collection;
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new DynamicToken(parameter, this);

        public Token this[string key] => Tokens[key];

        public Token At(string key) => Tokens.TryGetValue(key, out var token) ? token : null;

        public T As<T>()
        {
            var converter = cache.GetConverter<T>();
            var value = converter.ToValue(memory);
            return value;
        }

        public T As<T>(T anonymous) => As<T>();

        public object As(Type type)
        {
            if (type == null)
                ThrowHelper.ThrowArgumentNull();
            var converter = cache.GetConverter(type);
            var value = converter.ToValueAny(memory);
            return value;
        }

        #region override
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public sealed override bool Equals(object obj) => throw new NotSupportedException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public sealed override int GetHashCode() => throw new NotSupportedException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public sealed override string ToString() => $"{nameof(Token)}(Bytes: {memory.Length}, Items: {Tokens.Count})";
        #endregion
    }
}
