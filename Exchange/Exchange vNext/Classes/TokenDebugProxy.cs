using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Mikodev.Binary
{
    internal sealed class TokenDebugProxy
    {
        private readonly KeyValuePair<string, Token>[] items;

        public TokenDebugProxy(Token token) => items = token.Tokens.ToArray();

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<string, Token>[] Items => items;
    }
}
