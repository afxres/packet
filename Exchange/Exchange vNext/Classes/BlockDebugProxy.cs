using System.Diagnostics;

namespace Mikodev.Binary
{
    internal sealed class BlockDebugProxy
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public byte[] Items { get; }

        public BlockDebugProxy(Block block) => Items = block.ToArray();
    }
}
