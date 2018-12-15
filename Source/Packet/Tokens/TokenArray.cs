using System.Collections.Generic;

namespace Mikodev.Network.Tokens
{
    internal class TokenArray : Token
    {
        internal readonly List<Token> data;

        public override object Data => data;

        public TokenArray(List<Token> data)
        {
            this.data = data;
        }

        public override void FlushTo(Allocator context, int level)
        {
            if (data == null)
                return;
            for (var i = 0; i < data.Count; i++)
                context.AppendTokenExtend(data[i], level);
        }
    }
}
