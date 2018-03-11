using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal sealed class _EnumeratorGeneric<T> : IEnumerator, IEnumerator<T>
    {
        internal _Element _element;
        internal T _current = default(T);
        internal readonly IPacketConverter<T> _converter = null;

        internal _EnumeratorGeneric(_Element element, IPacketConverter<T> converter)
        {
            _element = new _Element(element);
            _converter = converter;
        }

        object IEnumerator.Current => _current;

        T IEnumerator<T>.Current => _current;

        public bool MoveNext()
        {
            if (_element.End())
                return false;
            _current = _element.NextGeneric(_converter);
            return true;
        }

        public void Reset()
        {
            _element._index = _element._offset;
            _current = default(T);
        }

        public void Dispose() { }
    }
}
