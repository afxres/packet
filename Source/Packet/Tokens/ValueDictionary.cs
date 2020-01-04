using System.Collections.Generic;

namespace Mikodev.Network.Tokens
{
    internal class ValueDictionary : Token
    {
        internal readonly int indexLength;

        internal readonly int elementLength;

        internal readonly List<KeyValuePair<byte[], byte[]>> data;

        public override object Data => this.data;

        public ValueDictionary(List<KeyValuePair<byte[], byte[]>> data, int indexLength, int elementLength)
        {
            this.data = data;
            this.indexLength = indexLength;
            this.elementLength = elementLength;
        }

        public override void FlushTo(Allocator context, int level)
        {
            if (this.data == null)
                return;
            for (var i = 0; i < this.data.Count; i++)
            {
                var item = this.data[i];
                if (this.indexLength > 0)
                    context.Append(item.Key);
                else
                    context.AppendValueExtend(item.Key);
                if (this.elementLength > 0)
                    context.Append(item.Value);
                else
                    context.AppendValueExtend(item.Value);
            }
        }
    }
}
