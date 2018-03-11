using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal class _Enumerable : IEnumerable
    {
        internal readonly _Element _element;
        internal readonly IPacketConverter _converter = null;

        internal _Enumerable(_Element element, IPacketConverter converter)
        {
            _element = element;
            _converter = converter;
        }

        IEnumerator IEnumerable.GetEnumerator() => new _Enumerator(_element, _converter);
    }

    internal sealed class _Enumerable<T> : _Enumerable, IEnumerable<T>
    {
        internal _Enumerable(_Element element, IPacketConverter converter) : base(element, converter) { }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            if (_converter is IPacketConverter<T> con)
                return new _EnumeratorGeneric<T>(_element, con);
            return new _Enumerator<T>(_element, _converter);
        }
    }
}
