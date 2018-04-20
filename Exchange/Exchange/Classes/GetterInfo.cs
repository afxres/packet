using System;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal sealed class GetterInfo
    {
        private readonly KeyValuePair<string, Type>[] infos;
        private readonly Action<object, object[]> action;

        internal GetterInfo(KeyValuePair<string, Type>[] infos, Action<object, object[]> action)
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
