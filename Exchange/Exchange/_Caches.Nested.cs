using System;

namespace Mikodev.Network
{
    partial class _Caches
    {
        internal sealed class _Wrapper<T>
        {
            internal T _val;

            internal T Value(object _) => _val;
        }

        internal sealed class _Emitter<S, R>
        {
            internal Func<S, R> _fun = null;

            internal object Value(object src) => _fun.Invoke((S)src);
        }

        internal static _Wrapper<T> _Wrap<T>(T val) => new _Wrapper<T> { _val = val };

        internal static _Emitter<S, R> _Emit<S, R>(Func<S, R> fun) => new _Emitter<S, R> { _fun = fun };
    }
}
