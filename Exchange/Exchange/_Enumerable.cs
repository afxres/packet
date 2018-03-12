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
            _element = new _Element(element);
            _converter = converter;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            var con = _converter;
            var ele = new _Element(_element);
            while (ele.Any())
                yield return ele.Next(con);
            yield break;
        }
    }

    internal sealed class _Enumerable<T> : _Enumerable, IEnumerable<T>
    {
        internal _Enumerable(_Element element, IPacketConverter converter) : base(element, converter) { }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            var con = _converter;
            var ele = new _Element(_element);
            if (con is IPacketConverter<T> gen)
                while (ele.Any())
                    yield return ele.NextGeneric(gen);
            else
                while (ele.Any())
                    yield return (T)ele.Next(con);
            yield break;
        }
    }
}
