using System;

namespace Mikodev.Binary.Common
{
    public abstract class VariableValueConverter : ValueConverter { }

    public abstract class VariableValueConverter<T>
    {
        public abstract void ToBytes(Allocator allocator, T value);

        public abstract T ToValue(Span<byte> block);
    }
}
