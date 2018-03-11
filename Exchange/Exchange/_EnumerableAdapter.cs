using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal sealed class _EnumerableAdapter<TK, TV> : IEnumerable<KeyValuePair<byte[], object>>
    {
        internal readonly IPacketConverter _key;
        internal readonly IEnumerable<KeyValuePair<TK, TV>> _pairs;

        internal _EnumerableAdapter(IPacketConverter key, IEnumerable<KeyValuePair<TK, TV>> pairs)
        {
            _key = key;
            _pairs = pairs;
        }

        public IEnumerator<KeyValuePair<byte[], object>> GetEnumerator()
        {
            if (_key is IPacketConverter<TK> gen)
            {
                foreach (var i in _pairs)
                {
                    var key = gen._GetBytesWrapErrorGeneric(i.Key);
                    var pair = new KeyValuePair<byte[], object>(key, i.Value);
                    yield return pair;
                }
            }
            else
            {
                foreach (var i in _pairs)
                {
                    var key = _key._GetBytesWrapError(i.Key);
                    var pair = new KeyValuePair<byte[], object>(key, i.Value);
                    yield return pair;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
