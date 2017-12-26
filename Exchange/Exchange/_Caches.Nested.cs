using System;

namespace Mikodev.Network
{
    partial class _Caches
    {
        internal struct _Info
        {
            internal string _name;
            internal Type _type;
        }

        internal sealed class _SolveInfo
        {
            internal Action<object, object[]> _func;
            internal _Info[] _args;
        }

        internal sealed class _DissoInfo
        {
            internal Func<object[], object> _func;
            internal _Info[] _args;
        }

        internal sealed class _Wrapper<T>
        {
            internal T _val;

            internal T Value(object _) => _val;
        }

        internal static _Wrapper<T> _Wrap<T>(T val) => new _Wrapper<T> { _val = val };
    }
}
