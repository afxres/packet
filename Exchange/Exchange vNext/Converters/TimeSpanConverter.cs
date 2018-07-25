using System;
using System.Runtime.CompilerServices;
using T = System.Int64;

namespace Mikodev.Binary.Converters
{
    internal sealed class TimeSpanConverter : Converter<TimeSpan>
    {
        public TimeSpanConverter() : base(Unsafe.SizeOf<T>()) { }

        public override void ToBytes(Allocator allocator, TimeSpan value) => UnmanagedValueConverter<T>.SafeToBytes(allocator, value.Ticks);

        public override TimeSpan ToValue(Memory<byte> memory) => new TimeSpan(UnmanagedValueConverter<T>.SafeToValue(memory));
    }
}
