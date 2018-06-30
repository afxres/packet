using System.Diagnostics;

namespace Mikodev.Binary
{
    internal sealed class BlockDebug
    {
        private readonly byte[] items;

        public BlockDebug(Block block) => items = block.ToArray();

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public byte[] Items => items;
    }
}
