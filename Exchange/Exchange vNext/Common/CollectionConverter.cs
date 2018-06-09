using System.Collections.Generic;

namespace Mikodev.Binary.Common
{
    public abstract class CollectionConverter : Converter { }

    public abstract class CollectionConverter<TCollection, TItem> : CollectionConverter where TCollection : IEnumerable<TItem>
    {
        public abstract TCollection ToCollection(IEnumerable<TItem> enumerable);
    }
}
