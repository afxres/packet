using Mikodev.Binary;
using Mikodev.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sample
{
    class Program
    {
        static void Main(string[] args)
        {
            var cache = new Cache();
            var dic = new Dictionary<string, List<TimeSpan>>();
            TraceWatch.InstanceDisposed = (tag, span) =>
            {
                if (dic.TryGetValue(tag, out var val))
                    val.Add(span);
                else dic.Add(tag, new List<TimeSpan>() { span });
            };

            const int max = 1 << 20;
            const int loop = 6;

            var abo = new
            {
                num = 1024,
                str = "string",
                arr = new[] { 7, 11, 555, 1313 },
                sub = new
                {
                    sum = 2.2D,
                    lst = new List<string> { "one", "two", "three" },
                }
            };

            var tmp = PacketConvert.Serialize(abo);

            for (int k = 0; k < loop; k++)
            {
                using (new TraceWatch("PacketCache Serialize")) // 780.808 ms
                {
                    for (int i = 0; i < max; i++)
                    {
                        var _ = cache.Serialize(abo);
                    }
                }

                using (new TraceWatch("PacketCache Deserialize")) // 1045.979 ms
                {
                    for (int i = 0; i < max; i++)
                    {
                        var _ = cache.Deserialize(tmp, abo);
                    }
                }

                using (new TraceWatch("PacketWriter")) // 1767.816 ms
                {
                    for (int i = 0; i < max; i++)
                    {
                        var _ = PacketConvert.Serialize(abo);
                    }
                }

                using (new TraceWatch("PacketReader")) // 1583.549 ms
                {
                    for (int i = 0; i < max; i++)
                    {
                        var _ = PacketConvert.Deserialize(tmp, abo);
                    }
                }
            }

            foreach (var i in dic)
            {
                var key = i.Key;
                var val = i.Value;
                if (val.Count > 4)
                    val.RemoveRange(0, 2);
                var sum = val.Select(r => r.Ticks).Sum();
                var cir = new TimeSpan(sum / val.Count);
                var avg = new TimeSpan(1000 * sum / val.Count / max);
                Console.WriteLine($"{key,-24} | " +
                    $"total: {new TimeSpan(sum).TotalMilliseconds,10:0.000} ms | " +
                    $"loop: {cir.TotalMilliseconds,10:0.000} ms | " +
                    $"avg: {avg.TotalMilliseconds,10:0.0000} ns");
            }
        }
    }

    static class Extension
    {
        internal static HashSet<T> ToHashSet<T>(this IEnumerable<T> enumerable) => new HashSet<T>(enumerable);
    }
}
