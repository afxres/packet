using Mikodev.Binary.Common;
using System;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary.Converters
{
    internal class UnmanagedValueConverter<T> : ConstantValueConverter<T> where T : unmanaged
    {
        internal UnmanagedValueConverter() : base(Unsafe.SizeOf<T>()) { }

        public override void ToBytes(Span<byte> block, T value) => Unsafe.WriteUnaligned(ref block[0], value);

        public override T ToValue(Span<byte> block) => Unsafe.ReadUnaligned<T>(ref block[0]);
    }
}
