using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal sealed class DictionaryAdapter<TK, TV> : IEnumerable<KeyValuePair<byte[], object>>
    {
        private readonly IPacketConverter converter;
        private readonly IEnumerable<KeyValuePair<TK, TV>> dictionary;

        internal DictionaryAdapter(IPacketConverter converter, IEnumerable<KeyValuePair<TK, TV>> dictionary)
        {
            this.converter = converter;
            this.dictionary = dictionary;
        }

        private static IEnumerator<KeyValuePair<byte[], object>> Enumerator(IEnumerable<KeyValuePair<TK, TV>> itr, IPacketConverter con)
        {
            if (con is IPacketConverter<TK> gen)
                foreach (var i in itr)
                    yield return new KeyValuePair<byte[], object>(gen.GetBytesWrap(i.Key), i.Value);
            else
                foreach (var i in itr)
                    yield return new KeyValuePair<byte[], object>(con.GetBytesWrap(i.Key), i.Value);
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator() => Enumerator(dictionary, converter);

        IEnumerator<KeyValuePair<byte[], object>> IEnumerable<KeyValuePair<byte[], object>>.GetEnumerator() => Enumerator(dictionary, converter);
    }
}
