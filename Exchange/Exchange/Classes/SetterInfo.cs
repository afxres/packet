using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal sealed class SetterInfo
    {
        private readonly KeyValuePair<string, Type>[] arguments;
        private Func<object[], object> func;

        internal SetterInfo(KeyValuePair<string, Type>[] arguments, Func<object[], object> func)
        {
            this.arguments = arguments;
            this.func = func;
        }

        internal KeyValuePair<string, Type>[] Arguments => arguments;

        internal object GetObject(object[] values) => func.Invoke(values);
    }
}
