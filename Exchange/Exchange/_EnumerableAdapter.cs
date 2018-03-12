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
            var con = _key;
            var itr = _pairs;
            if (con is IPacketConverter<TK> gen)
                foreach (var i in itr)
                    yield return new KeyValuePair<byte[], object>(gen._GetBytesWrapErrorGeneric(i.Key), i.Value);
            else
                foreach (var i in itr)
                    yield return new KeyValuePair<byte[], object>(con._GetBytesWrapError(i.Key), i.Value);
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
