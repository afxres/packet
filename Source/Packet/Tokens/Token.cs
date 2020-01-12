using Mikodev.Network.Internal;

namespace Mikodev.Network.Tokens
{
    internal abstract class Token
    {
        public static readonly Token Empty = new Empty();

        public abstract object Data { get; }

        public abstract void FlushTo(Allocator context, int level);
    }
}
