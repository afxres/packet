using System.Collections.Generic;

namespace Mikodev.Network.Tokens
{
    internal class ValueDictionary : Token
    {
        internal readonly int indexLength;

        internal readonly int elementLength;

        internal readonly List<KeyValuePair<byte[], byte[]>> data;

        public override object Data => data;

        public ValueDictionary(List<KeyValuePair<byte[], byte[]>> data, int indexLength, int elementLength)
        {
            this.data = data;
            this.indexLength = indexLength;
            this.elementLength = elementLength;
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
                if (elementLength > 0)
                    context.Append(item.Value);
                else
                    context.AppendValueExtend(item.Value);
            }
        }
    }
}
