using System;

namespace Mikodev.Network
{
    partial class _Caches
    {
        internal struct Info
        {
            internal string name;
            internal Type type;
        }

        internal sealed class SolveInfo
        {
            internal Action<object, object[]> func;
            internal Info[] args;
        }

        internal sealed class DissoInfo
        {
            internal Func<object[], object> func;
            internal Info[] args;
        }

        internal sealed class Wrapper<T>
        {
            internal T val;
            internal T Value(object _) => val;
        }

        internal static Wrapper<T> Wrap<T>(T val) => new Wrapper<T> { val = val };
    }
}
