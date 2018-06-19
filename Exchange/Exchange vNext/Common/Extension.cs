using System.Text;

namespace Mikodev.Binary.Common
{
    internal static class Extension
    {
        internal static readonly Encoding Encoding = Encoding.UTF8;

        internal static int TupleLength(params Converter[] converters)
        {
            var total = 0;
            foreach (var i in converters)
            {
                if (i.Length == 0)
                    return 0;
                total += i.Length;
            }
            return total;
        }
    }
}
