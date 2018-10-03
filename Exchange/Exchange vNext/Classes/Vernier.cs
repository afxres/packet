using Mikodev.Binary.Converters;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Mikodev.Binary
{
    internal ref struct Vernier
    {
        internal static MethodInfo UpdateExceptMethodInfo = typeof(Vernier).GetMethod(nameof(UpdateExcept), BindingFlags.Instance | BindingFlags.NonPublic);

        internal static FieldInfo OffsetFieldInfo = typeof(Vernier).GetField(nameof(offset), BindingFlags.Instance | BindingFlags.NonPublic);

        internal static FieldInfo LengthFieldInfo = typeof(Vernier).GetField(nameof(length), BindingFlags.Instance | BindingFlags.NonPublic);

        internal readonly unsafe byte* pointer;

        internal readonly int limits;

        internal int offset;

        internal int length;

        internal unsafe Vernier(byte* pointer, int limits)
        {
            this.pointer = pointer;
            this.limits = limits;
            offset = 0;
            length = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal bool Any() => limits - offset != length;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal unsafe void Update()
        {
            offset += this.length;
            if ((uint)(limits - offset) < sizeof(int))
                ThrowHelper.ThrowOverflow();
            var length = UnmanagedValueConverter<int>.UnsafeToValue(pointer + offset);
            offset += sizeof(int);
            if ((uint)(limits - offset) < (uint)length)
                ThrowHelper.ThrowOverflow();
            this.length = length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void UpdateExcept(int define)
        {
            if (define == 0)
            {
                Update();
            }
            else
            {
                offset += length;
                if ((uint)(limits - offset) < (uint)define)
                    ThrowHelper.ThrowOverflow();
                length = define;
            }
        }
    }
}
