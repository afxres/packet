using System;

namespace Mikodev.Network
{
    partial class _Caches
    {
        internal struct AccessorInfo
        {
            internal string Name { get; set; }

            internal Type Type { get; set; }
        }

        internal struct GetterInfo
        {
            internal Action<object, object[]> Function { get; set; }

            internal AccessorInfo[] Arguments { get; set; }
        }

        internal struct SetterInfo
        {
            internal Func<object[], object> Function { get; set; }

            internal AccessorInfo[] Arguments { get; set; }
        }
    }
}
