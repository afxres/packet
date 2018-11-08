using System;

namespace Mikodev.Binary
{
    internal delegate T ToValue<out T>(ReadOnlySpan<byte> memory);

    internal delegate void ToBytes<in T>(Allocator allocator, T value);

    internal delegate T ToValueFixed<out T>(ReadOnlySpan<byte> memory, Vernier vernier);

    internal delegate T ToValueExpando<out T>(ReadOnlySpan<byte> memory, HybridDictionary entries);
}
