using System;
using T = System.Int64;

namespace Mikodev.Binary.Converters
{
    internal sealed class DateTimeConverter : Converter<DateTime>
    {
        public unsafe DateTimeConverter() : base(sizeof(T)) { }

        public override void ToBytes(Allocator allocator, DateTime value) => UnmanagedValueConverter<T>.Bytes(allocator, value.ToBinary());

        public override DateTime ToValue(ReadOnlyMemory<byte> memory) => DateTime.FromBinary(UnmanagedValueConverter<T>.Value(memory));
    }
}
