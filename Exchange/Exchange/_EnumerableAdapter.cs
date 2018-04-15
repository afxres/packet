using System.Collections;
using System.Collections.Generic;

namespace Mikodev.Network
{
    internal sealed class _EnumerableAdapter<T> : IEnumerable<T>
    {
        private readonly PacketReader rea;
        private readonly int lev;
        private readonly _Inf inf;

        internal _EnumerableAdapter(PacketReader rea, int lev, _Inf inf)
        {
            this.rea = rea;
            this.lev = lev;
            this.inf = inf;
        }

        private IEnumerator<T> Enumerable()
        {
            var arr = rea.GetArray();
            for (int i = 0; i < arr.Length; i++)
                yield return (T)arr[i].GetValueMatch(typeof(T), lev, inf);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() => Enumerable();

        IEnumerator IEnumerable.GetEnumerator() => Enumerable();
    }
}
