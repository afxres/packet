using Mikodev.Network.Internal;

namespace Mikodev.Network.Tokens
{
    internal class ValueArray : Token
    {
        internal readonly byte[][] data;

        internal readonly int length;

        public override object Data => this.data;

        public ValueArray(byte[][] data, int length)
        {
            this.data = data;
            this.length = length;
        }

        public override void FlushTo(Allocator context, int level)
        {
            if (this.data == null)
                return;
            if (this.length > 0)
                for (var i = 0; i < this.data.Length; i++)
                    context.Append(this.data[i]);
            else
                for (var i = 0; i < this.data.Length; i++)
                    context.AppendValueExtend(this.data[i]);
        }
    }
}
