namespace Mikodev.Network.Tokens
{
    internal class Value : Token
    {
        private readonly byte[] data;

        public override object Data => data;

        public Value(byte[] data)
        {
            this.data = data;
        }

        public override void FlushTo(Allocator context, int level)
        {
            if (data == null)
                return;
            context.Append(data);
        }
    }
}
