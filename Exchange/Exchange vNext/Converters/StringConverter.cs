namespace Mikodev.Binary.Converters
{
    internal sealed class StringConverter : Converter<string>
    {
        public StringConverter() : base(0) { }

        public override void ToBytes(Allocator allocator, string value)
        {
            allocator.Append(value);
        }

        public override string ToValue(Block block)
        {
            return block.Length == 0 ? string.Empty : Encoding.GetString(block.Buffer, block.Offset, block.Length);
        }
    }
}
