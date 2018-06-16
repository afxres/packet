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
            var builder = new PacketCache();
            //{
            //    var c = Enumerable.Range(0, 8).Select(r => new { id = r, text = r.ToString("x4") });
            //    var v = new { array = c.ToArray(), list = c.ToList() };
            //    var ta = builder.Serialize(v);
            //    var tb = PacketConvert.Serialize(v);
            //    var ra = PacketConvert.Deserialize(ta, v);
            //    var rb = PacketConvert.Deserialize(tb, v);
            //}

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

            for (int k = 0; k < loop; k++)
            {
                using (new TraceWatch("PacketBuilder"))
                {
                    for (int i = 0; i < max; i++)
                    {
                        var buffer = builder.Serialize(anonymous);
                    }
                }

                //using (new TraceWatch("PacketWriter"))
                //{
                //    for (int i = 0; i < max; i++)
                //    {
                //        var buffer = PacketConvert.Serialize(anonymous);
                //    }
                //}
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
}
