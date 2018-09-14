using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal sealed class SetInfo
    {
        internal KeyValuePair<string, Type>[] Arguments { get; }

        internal Func<object[], object> Functor { get; }

        internal SetInfo(KeyValuePair<string, Type>[] arguments, Func<object[], object> functor)
        {
            Arguments = arguments;
            Functor = functor;
        }
    }
}
