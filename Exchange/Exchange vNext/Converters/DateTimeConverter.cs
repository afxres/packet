using System;
using T = System.Int64;

namespace Mikodev.Binary.Converters
{
    internal sealed class DateTimeConverter : Converter<DateTime>
    {
        public unsafe DateTimeConverter() : base(sizeof(T)) { }

        public override void ToBytes(ref Allocator allocator, DateTime value) => UnmanagedValueConverter<T>.Bytes(ref allocator, value.ToBinary());

        public override DateTime ToValue(ReadOnlySpan<byte> memory) => DateTime.FromBinary(UnmanagedValueConverter<T>.Value(memory));
    }
}
