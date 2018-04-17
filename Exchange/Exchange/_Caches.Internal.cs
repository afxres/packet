using System;

namespace Mikodev.Network
{
    partial class _Caches
    {
        internal struct AccessorInfo
        {
            internal string Name { get; set; }

            internal Type Type { get; set; }
        }

        internal struct GetterInfo
        {
            private readonly AccessorInfo[] array;
            private readonly Action<object, object[]> action;

            internal GetterInfo(AccessorInfo[] infos, Action<object, object[]> function)
            {
                array = infos;
                action = function;
            }

            internal AccessorInfo[] Arguments => array;

            internal Action<object, object[]> Function => action;

            internal object[] GetValues(object value)
            {
                var result = new object[array.Length];
                action.Invoke(value, result);
                return result;
            }
        }

        internal struct SetterInfo
        {
            internal AccessorInfo[] Arguments { get; set; }

            internal Func<object[], object> Function { get; set; }
        }
    }
}
