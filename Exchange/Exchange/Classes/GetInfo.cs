using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal sealed class GetInfo
    {
        internal KeyValuePair<string, Type>[] Arguments { get; }

        internal Action<object, object[]> Functor { get; }

        internal GetInfo(KeyValuePair<string, Type>[] arguments, Action<object, object[]> functor)
        {
            Arguments = arguments;
            Functor = functor;
        }
    }
}
