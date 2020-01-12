using Mikodev.Network.Internal;

namespace Mikodev.Network.Tokens
{
    internal class Value : Token
    {
        private readonly byte[] data;

        public override object Data => this.data;

        public Value(byte[] data)
        {
            this.data = data;
        }

        public override void FlushTo(Allocator context, int level)
        {
            if (this.data == null)
                return;
            context.Append(this.data);
        }
    }
}
