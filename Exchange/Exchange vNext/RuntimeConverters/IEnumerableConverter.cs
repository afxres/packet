using System;

namespace Mikodev.Binary.RuntimeConverters
{
    internal interface IEnumerableConverter
    {
        Delegate GetToEnumerableDelegate();
    }
}
