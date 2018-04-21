using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal sealed class GetInfo
    {
        private readonly KeyValuePair<string, Type>[] infos;
        private readonly Action<object, object[]> action;

        internal GetInfo(KeyValuePair<string, Type>[] infos, Action<object, object[]> action)
        {
            this.infos = infos;
            this.action = action;
        }

        internal KeyValuePair<string, Type>[] Arguments => infos;

        internal object[] GetValues(object value)
        {
            var result = new object[infos.Length];
            action.Invoke(value, result);
            return result;
        }
    }
}
