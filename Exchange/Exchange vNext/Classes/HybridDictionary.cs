using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using TValue = Mikodev.Binary.Segment;

namespace Mikodev.Binary
{
    internal readonly struct HybridDictionary
    {
        internal static MethodInfo GetValueMethodInfo = typeof(HybridDictionary).GetMethod(nameof(GetValue), BindingFlags.Instance | BindingFlags.NonPublic);

        private const int limits = 8;

        private readonly Dictionary<string, TValue> dictionary;

        private readonly List<KeyValuePair<string, TValue>> collection;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal HybridDictionary(int capacity)
        {
            dictionary = capacity > limits ? new Dictionary<string, TValue>(capacity) : default;
            collection = capacity > limits ? default : new List<KeyValuePair<string, TValue>>(capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void Add(string key, TValue value)
        {
            if (dictionary != null)
                dictionary.Add(key, value);
            else
                collection.Add(new KeyValuePair<string, TValue>(key, value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal TValue GetValue(string key)
        {
            if (dictionary != null)
                return dictionary[key];
            var size = collection.Count;
            var item = default(KeyValuePair<string, TValue>);
            for (var i = 0; i < size; i++)
                if (string.Equals((item = collection[i]).Key, key))
                    return item.Value;
            return ThrowHelper.ThrowKeyNotFoundException<TValue>();
        }
    }
}
