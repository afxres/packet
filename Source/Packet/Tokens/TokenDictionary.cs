using System.Collections.Generic;

namespace Mikodev.Network.Tokens
{
    internal class TokenDictionary : Token
    {
        internal readonly int indexLength;

        internal readonly List<KeyValuePair<byte[], Token>> data;

        public override object Data => data;

        public TokenDictionary(List<KeyValuePair<byte[], Token>> data, int indexLength)
        {
            this.data = data;
            this.indexLength = indexLength;
        }

        public override void FlushTo(Allocator context, int level)
        {
            if (data == null)
                return;
            for (var i = 0; i < data.Count; i++)
            {
                var item = data[i];
                if (indexLength > 0)
                    context.Append(item.Key);
                else
                    context.AppendValueExtend(item.Key);
                context.AppendTokenExtend(item.Value, level);
            }
        }
    }
}
