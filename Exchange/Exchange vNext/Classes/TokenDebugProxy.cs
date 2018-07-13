using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Mikodev.Binary
{
    internal sealed class TokenDebugProxy
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public KeyValuePair<string, Token>[] Items { get; }

        public TokenDebugProxy(Token token) => Items = token.Tokens.ToArray();
    }
}
