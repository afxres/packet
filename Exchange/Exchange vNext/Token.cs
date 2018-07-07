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
        private readonly Block block;
        private Dictionary<string, Token> dictionary;

        internal Dictionary<string, Token> Tokens => dictionary ?? GetDictionary();

        internal Token(Cache cache, Block block)
        {
            this.cache = cache;
            this.block = block;
        }

        private Dictionary<string, Token> GetDictionary()
        {
            var map = default(Dictionary<string, Token>);
            var vernier = (Vernier)block;

            try
            {
                while (vernier.Any)
                {
                    vernier.Flush();
                    var key = Converter.Encoding.GetString(vernier.Buffer, vernier.Offset, vernier.Length);
                    vernier.Flush();
                    var value = new Token(cache, (Block)vernier);
                    if (map == null)
                        map = new Dictionary<string, Token>(8);
                    map.Add(key, value);
                }
            }
            catch (Exception)
            {
                map = null;
            }

            if (map == null)
                map = empty;
            dictionary = map;
            return map;
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new DynamicToken(parameter, this);

        public Token this[string key] => Tokens[key];

        public Token At(string key) => Tokens.TryGetValue(key, out var token) ? token : null;

        public T As<T>()
        {
            var converter = cache.GetConverter<T>();
            var value = converter.ToValue(block);
            return value;
        }

        public T As<T>(T anonymous) => As<T>();

        public object As(Type type)
        {
            if (type == null)
                ThrowHelper.ThrowArgumentNull();
            var converter = cache.GetConverter(type);
            var value = converter.ToValueAny(block);
            return value;
        }

        #region override
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool Equals(object obj) => throw new InvalidOperationException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode() => throw new InvalidOperationException();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override string ToString() => $"{nameof(Token)} item count : {Tokens.Count}, byte length : {block.Length}";
        #endregion
    }
}
