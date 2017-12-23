using System;

namespace Mikodev.Network
{
    partial class _Caches
    {
        internal struct _Get
        {
            internal string _name;
            internal Func<object, object> _func;
        }

        internal struct _Anon
        {
            internal string _name;
            internal Type _type;
        }

        internal struct _Set
        {
            internal string _name;
            internal Type _type;
            internal Action<object, object> _func;
        }

        internal sealed class _AnonInfo
        {
            /* Anonymous type constructor and parameter list */
            internal Func<object[], object> _func;
            internal _Anon[] _args;
        }

        internal sealed class _SetInfo
        {
            internal Func<object> _func;
            internal _Set[] _sets;
        }

        internal sealed class _Wrapper<T>
        {
            internal T _val;

            internal T Value(object _) => _val;
        }

        internal static _Wrapper<T> _Wrap<T>(T val) => new _Wrapper<T> { _val = val };
    }
}
