using System;

namespace Mikodev.Network
{
    partial class _Caches
    {
        internal struct AccessorInfo
        {
            internal string name;
            internal Type type;
        }

        internal sealed class GetterInfo
        {
            internal Action<object, object[]> func;
            internal AccessorInfo[] args;
        }

        internal sealed class SetterInfo
        {
            internal Func<object[], object> func;
            internal AccessorInfo[] args;
        }
    }
}
