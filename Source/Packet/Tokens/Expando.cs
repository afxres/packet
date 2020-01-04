using System.Collections.Generic;

namespace Mikodev.Network.Tokens
{
    internal class Expando : Token
    {
        internal readonly Dictionary<string, PacketWriter> data;

        public override object Data => this.data;

        public Expando(Dictionary<string, PacketWriter> data)
        {
            this.data = data;
        }

        public override void FlushTo(Allocator context, int level)
        {
            if (this.data == null)
                return;
            foreach (var i in this.data)
            {
                context.AppendKey(i.Key);
                context.AppendTokenExtend(i.Value.token, level);
            }
        }
    }
}
