using Mikodev.Binary.Common;
using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary.Converters
{
    internal class UnmanagedValueConverter<T> : ValueConverter<T> where T : unmanaged
    {
        public UnmanagedValueConverter() : base(Unsafe.SizeOf<T>()) { }

        public override void ToBytes(Allocator allocator, T value) => Unsafe.WriteUnaligned(ref allocator.Allocate(Unsafe.SizeOf<T>())[0], value);

        public override T ToValue(Span<byte> block) => Unsafe.ReadUnaligned<T>(ref block[0]);
    }
}
