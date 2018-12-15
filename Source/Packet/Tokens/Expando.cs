using System.Collections.Generic;

namespace Mikodev.Network.Tokens
{
    internal class Expando : Token
    {
        internal readonly Dictionary<string, PacketWriter> data;

        public override object Data => data;

        public Expando(Dictionary<string, PacketWriter> data)
        {
            this.data = data;
        }

        public override void FlushTo(Allocator context, int level)
        {
            if (data == null)
                return;
            foreach (var i in data)
            {
                context.AppendKey(i.Key);
                context.AppendTokenExtend(i.Value.token, level);
            }
        }
    }
}
