using Mikodev.Network.Internal;

namespace Mikodev.Network.Tokens
{
    internal class Empty : Token
    {
        public override object Data => null;

        public override void FlushTo(Allocator context, int level) { /* ignore */ }
    }
}
