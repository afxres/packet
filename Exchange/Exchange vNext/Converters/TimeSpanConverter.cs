using System;
using T = System.Int64;

namespace Mikodev.Binary.Converters
{
    internal sealed class TimeSpanConverter : Converter<TimeSpan>
    {
        public unsafe TimeSpanConverter() : base(sizeof(T)) { }

        public override void ToBytes(Allocator allocator, TimeSpan value) => UnmanagedValueConverter<T>.Bytes(allocator, value.Ticks);

        public override TimeSpan ToValue(ReadOnlySpan<byte> memory) => new TimeSpan(UnmanagedValueConverter<T>.Value(memory));
    }
}
