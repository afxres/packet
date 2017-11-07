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

        internal sealed class _Emitter<T, R>
        {
            internal Func<T, R> _fun = null;

            internal object Value(object src) => _fun.Invoke((T)src);
        }

        internal static _Wrapper<T> _Wrap<T>(T val) => new _Wrapper<T> { _val = val };

        internal static _Emitter<T, R> _Emit<T, R>(Func<T, R> fun) => new _Emitter<T, R> { _fun = fun };
    }
}
