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

        private Dictionary<string, Token> tokens;

        internal Token(Cache cache, ReadOnlyMemory<byte> memory)
        {
            if (memory.IsEmpty)
                tokens = empty;
            else
                this.memory = memory;
            this.cache = cache;
        }

        internal Dictionary<string, Token> GetTokens() => tokens ?? GetDictionary();

        private unsafe Dictionary<string, Token> GetDictionary()
        {
            fixed (byte* srcptr = memory.Span)
            {
                var vernier = new Vernier(srcptr, memory.Length);
                var dictionary = new Dictionary<string, Token>(8);

                try
                {
                    while (vernier.Any())
                    {
                        vernier.Update();
                        var key = Converter.Encoding.GetString(srcptr + vernier.offset, vernier.length);
                        vernier.Update();
                        var value = new Token(cache, memory.Slice(vernier.offset, vernier.length));
                        dictionary.Add(key, value);
                    }

                    tokens = dictionary;
                    return dictionary;
                }
                catch (Exception ex) when (ex is ArgumentException || ex is OverflowException) { }
            }

            tokens = empty;
            return empty;
        }

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter) => new DynamicToken(parameter, this);

        public Token this[string key] => GetTokens()[key];

        public Token At(string key) => GetTokens().TryGetValue(key, out var token) ? token : null;

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
        public sealed override string ToString() => $"{nameof(Token)}(Items: {GetTokens().Count}, Bytes: {memory.Length})";
        #endregion
    }
}
