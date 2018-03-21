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
            private readonly AccessorInfo[] _arr;
            private readonly Action<object, object[]> _act;

            internal GetterInfo(AccessorInfo[] infos, Action<object, object[]> action)
            {
                _arr = infos;
                _act = action;
            }

            internal AccessorInfo[] Arguments => _arr;

            internal Action<object, object[]> Function => _act;

            internal object[] GetValues(object value)
            {
                var result = new object[_arr.Length];
                _act.Invoke(value, result);
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
