using System;

namespace Mikodev.Binary.RuntimeConverters
{
    internal sealed class TupleConverter<T> : Converter<T>
    {
        private readonly int[] definitions;

        private readonly Action<Allocator, T> toBytes;

        private readonly Func<ReadOnlyMemory<byte>[], T> toValue;

        public TupleConverter(Action<Allocator, T> toBytes, Func<ReadOnlyMemory<byte>[], T> toValue, int[] definitions, int length) : base(length)
        {
            this.toBytes = toBytes;
            this.toValue = toValue;
            this.definitions = definitions;
        }

        public override void ToBytes(Allocator allocator, T value) => toBytes.Invoke(allocator, value);

        public override unsafe T ToValue(ReadOnlyMemory<byte> memory)
        {
            var length = definitions.Length;
            var result = new ReadOnlyMemory<byte>[length];
            fixed (byte* pointer = &memory.Span[0])
            {
                var vernier = new Vernier(pointer, memory.Length);
                for (var i = 0; i < length; i++)
                {
                    vernier.FlushExcept(definitions[i]);
                    var slice = memory.Slice(vernier.offset, vernier.length);
                    result[i] = slice;
                }
            }
            return toValue.Invoke(result);
        }
    }
}
