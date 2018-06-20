using Mikodev.Binary;
using Mikodev.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sample
{
    class OneTwo<TOne, TTwo>
    {
        public TOne One { get; set; }

        public TTwo Two { get; set; }

        public override string ToString() => $"one : {One}, two : {Two}";
    }

    class Program
    {
        static void Main(string[] args)
        {
            var cache = new Cache();
            {
                var c = Enumerable.Range(0, 8).Select(r => new OneTwo<int, string> { One = r, Two = r.ToString("x4") });
                var v = new
                {
                    array = c.ToArray(),
                    list = c.ToList(),
                    set = (ISet<OneTwo<int, string>>)c.ToHashSet(),
                    enumerable = c,
                    dictionary = c.ToDictionary(r => (-r.One)),
                };
                var ta = cache.Serialize(v);
                var tb = PacketConvert.Serialize(v);
                var ra = cache.Deserialize(ta, v);
                var rb = PacketConvert.Deserialize(tb, v);

                var obj = (object)new { id = 1024, name = "sharp", data = (object)new { age = 18 } };
                var tc = cache.Serialize(obj);
                var td = PacketConvert.Serialize(obj);
                var rc = new PacketReader(tc);
                var rd = new PacketReader(td);

                var tup = new
                {
                    a = Tuple.Create("empty", Array.Empty<int>()),
                    b = ValueTuple.Create(1.1, "one dot one"),
                    c = ValueTuple.Create(1, 2L, 3.0F, 4.0D),
                };
                var te = cache.Serialize(tup);
                var re = cache.Deserialize(te, tup);
            }

            var dic = new Dictionary<string, List<TimeSpan>>();
            TraceWatch.InstanceDisposed = (tag, span) =>
            {
                if (dic.TryGetValue(tag, out var val))
                    val.Add(span);
                else dic.Add(tag, new List<TimeSpan>() { span });
            };

            const int max = 1 << 20;
            const int loop = 6;

            var anonymous = new
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
            var tx = PacketConvert.Serialize(anonymous);
            var ty = cache.Serialize(anonymous);
            var rx = PacketConvert.Deserialize(tx, anonymous);
            var ry = cache.Deserialize(ty, anonymous);

            for (int k = 0; k < loop; k++)
            {
                using (new TraceWatch("PacketCache Serialize")) // 830.489 ms
                {
                    for (int i = 0; i < max; i++)
                    {
                        var buffer = cache.Serialize(anonymous);
                    }
                }

                using (new TraceWatch("PacketWriter")) // 1783.173 ms
                {
                    for (int i = 0; i < max; i++)
                    {
                        var buffer = PacketConvert.Serialize(anonymous);
                    }
                }

                using (new TraceWatch("PacketCache Deserialize")) // 1057.535 ms
                {
                    for (int i = 0; i < max; i++)
                    {
                        var _ = cache.Deserialize(tx, anonymous);
                    }
                }

                using (new TraceWatch("PacketReader")) // 1579.233 ms
                {
                    for (int i = 0; i < max; i++)
                    {
                        var _ = PacketConvert.Deserialize(tx, anonymous);
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
