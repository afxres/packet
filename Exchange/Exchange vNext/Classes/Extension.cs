using System.Runtime.CompilerServices;
using System.Text;

namespace Mikodev.Binary
{
    internal static class Extension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static unsafe string GetString(this Encoding encoding, ref byte location, int length)
        {
            fixed (byte* pointer = &location)
                return encoding.GetString(pointer, length);
        }
    }
}
