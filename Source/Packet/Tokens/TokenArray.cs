using System.Collections.Generic;

namespace Mikodev.Network.Tokens
{
    internal class TokenArray : Token
    {
        internal readonly List<Token> data;

        public override object Data => this.data;

        public TokenArray(List<Token> data)
        {
            this.data = data;
        }

        public override void FlushTo(Allocator context, int level)
        {
            if (this.data == null)
                return;
            for (var i = 0; i < this.data.Count; i++)
                context.AppendTokenExtend(this.data[i], level);
        }
    }
}
