using System.Diagnostics;

namespace Mikodev.Binary
{
    internal sealed class BlockDebugProxy
    {
        private readonly byte[] items;

        public BlockDebugProxy(Block block) => items = block.ToArray();

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public byte[] Items => items;
    }
}
