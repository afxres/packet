using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal sealed class _EnumerableAdapter<TK, TV> : IEnumerable<KeyValuePair<byte[], object>>
    {
        private readonly IPacketConverter _con;
        private readonly IEnumerable<KeyValuePair<TK, TV>> _kvp;

        internal _EnumerableAdapter(IPacketConverter key, IEnumerable<KeyValuePair<TK, TV>> pairs)
        {
            _con = key;
            _kvp = pairs;
        }

        private static IEnumerator<KeyValuePair<byte[], object>> _Enumerator(IEnumerable<KeyValuePair<TK, TV>> itr, IPacketConverter con)
        {
            if (con is IPacketConverter<TK> gen)
                foreach (var i in itr)
                    yield return new KeyValuePair<byte[], object>(gen.GetBytesWrap(i.Key), i.Value);
            else
                foreach (var i in itr)
                    yield return new KeyValuePair<byte[], object>(con.GetBytesWrap(i.Key), i.Value);
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator() => _Enumerator(_kvp, _con);

        IEnumerator<KeyValuePair<byte[], object>> IEnumerable<KeyValuePair<byte[], object>>.GetEnumerator() => _Enumerator(_kvp, _con);
    }
}
