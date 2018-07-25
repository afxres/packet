using System;
using System.Runtime.CompilerServices;
using T = System.Int64;

namespace Mikodev.Binary.Converters
{
    internal sealed class DateTimeConverter : Converter<DateTime>
    {
        public DateTimeConverter() : base(Unsafe.SizeOf<T>()) { }

        public override void ToBytes(Allocator allocator, DateTime value) => UnmanagedValueConverter<T>.SafeToBytes(allocator, value.ToBinary());

        public override DateTime ToValue(Memory<byte> memory) => DateTime.FromBinary(UnmanagedValueConverter<T>.SafeToValue(memory));
    }
}
