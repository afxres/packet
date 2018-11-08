using System.Reflection;

namespace Mikodev.Binary
{
    internal readonly struct Segment
    {
        internal static FieldInfo OffsetFieldInfo = typeof(Segment).GetField(nameof(offset), BindingFlags.Instance | BindingFlags.NonPublic);

        internal static FieldInfo LengthFieldInfo = typeof(Segment).GetField(nameof(length), BindingFlags.Instance | BindingFlags.NonPublic);

        internal readonly int offset;

        internal readonly int length;

        public Segment(int offset, int length)
        {
            this.offset = offset;
            this.length = length;
        }
    }
}
