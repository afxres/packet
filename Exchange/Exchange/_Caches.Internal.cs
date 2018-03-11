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
            private readonly AccessorInfo[] _infos;
            private readonly Action<object, object[]> _action;

            internal GetterInfo(AccessorInfo[] infos, Action<object, object[]> action)
            {
                _infos = infos;
                _action = action;
            }

            internal AccessorInfo[] Arguments => _infos;

            internal Action<object, object[]> Function => _action;

            internal object[] GetValues(object value)
            {
                var result = new object[_infos.Length];
                _action.Invoke(value, result);
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
