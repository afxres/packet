using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    partial class _Caches
    {
        internal struct _KeyValue<K, V>
        {
            internal K _key;
            internal V _value;
        }

        internal sealed class _AnonInfo
        {
            /* Anonymous type constructor and parameter list */
            internal Func<object[], object> _func;
            internal _KeyValue<string, Type>[] _args;
        }

        internal sealed class _Wrapper<T>
        {
            internal T _val;

            internal T Value(object _) => _val;
        }

        internal sealed class _Emitter<T, R>
        {
            internal Func<T, R> _fun = null;

            internal object Value(object src) => _fun.Invoke((T)src);
        }

        internal static _Wrapper<T> _Wrap<T>(T val) => new _Wrapper<T> { _val = val };

        internal static _Emitter<T, R> _Emit<T, R>(Func<T, R> fun) => new _Emitter<T, R> { _fun = fun };
    }
}
