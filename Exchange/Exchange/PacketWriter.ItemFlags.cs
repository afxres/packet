namespace Mikodev.Network
{
    public partial class PacketWriter
    {
        internal enum ItemFlags : int
        {
            None = 0,
            Buffer,
            BufferArray,
            ItemList,
            Dictionary,
            DictionaryBuffer,
            DictionaryBufferItem,
        }
    }
}
