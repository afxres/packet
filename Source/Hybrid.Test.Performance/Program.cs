using Mikodev.Binary;
using Mikodev.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Sample
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var obj = new
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

            const int max = 1 << 20;
            const int loop = 6;

            var cache = new Cache();
            var result = new Dictionary<string, List<TimeSpan>>();

            var bytes = PacketConvert.Serialize(obj);
            var converter = cache.GetConverter(obj);
            var arrayPool = new byte[4096];

            TraceWatch.InstanceDisposed = (tag, span) =>
            {
                if (result.TryGetValue(tag, out var val))
                    val.Add(span);
                else result.Add(tag, new List<TimeSpan>() { span });
            };

            /* i7 7700hq, .net core 2.1, release */

            for (var k = 0; k < loop; k++)
            {
                using (new TraceWatch("BitConverter")) // 5.099 ms
                {
                    for (var i = 0; i < max; i++)
                    {
                        var buffer = BitConverter.GetBytes(i);
                        var _ = BitConverter.ToInt32(buffer, 0);
                    }
                }

                using (new TraceWatch("PacketWriter")) // 1433.147 ms
                {
                    for (var i = 0; i < max; i++)
                    {
                        var writer = new PacketWriter()
                            .SetValue(nameof(obj.num), obj.num)
                            .SetValue(nameof(obj.str), obj.str)
                            .SetValue(nameof(obj.arr), obj.arr)
                            .SetItem(nameof(obj.sub), new PacketWriter()
                                .SetValue(nameof(obj.sub.sum), obj.sub.sum)
                                .SetEnumerable(nameof(obj.sub.lst), obj.sub.lst));
                        var _ = writer.GetBytes();
                    }
                }

                using (new TraceWatch("ToBytes")) // 343.723 ms
                {
                    for (var i = 0; i < max; i++)
                    {
                        var _ = cache.ToBytes(obj);
                    }
                }

                using (new TraceWatch("ToBytes (Converter)")) // 273.200 ms
                {
                    for (var i = 0; i < max; i++)
                    {
                        var allocator = new Allocator(arrayPool);
                        converter.ToBytes(ref allocator, obj);
                        var _ = allocator.ToArray();
                    }
                }

                using (new TraceWatch("ToValue")) // 693.167 ms
                {
                    for (var i = 0; i < max; i++)
                    {
                        var _ = cache.ToValue(bytes, obj);
                    }
                }

                using (new TraceWatch("ToValue (Converter)")) // 649.977 ms
                {
                    for (var i = 0; i < max; i++)
                    {
                        var _ = converter.ToValue(bytes);
                    }
                }

                using (new TraceWatch("Packet Serialize")) // 1736.535 ms
                {
                    for (var i = 0; i < max; i++)
                    {
                        var _ = PacketConvert.Serialize(obj);
                    }
                }

                using (new TraceWatch("Packet Deserialize")) // 1698.635 ms
                {
                    for (var i = 0; i < max; i++)
                    {
                        var _ = PacketConvert.Deserialize(bytes, obj);
                    }
                }
            }

            foreach (var i in result)
            {
                var key = i.Key;
                var value = i.Value;
                if (value.Count > 4)
                    value.RemoveRange(0, 2);
                var total = value.Select(r => r.Ticks).Sum();
                var circles = new TimeSpan(total / value.Count);
                var average = new TimeSpan(1000 * total / value.Count / max);
                Console.WriteLine($"{key,-24} | " +
                    $"total: {new TimeSpan(total).TotalMilliseconds,10:0.000} ms | " +
                    $"loop: {circles.TotalMilliseconds,10:0.000} ms | " +
                    $"avg: {average.TotalMilliseconds,10:0.0000} ns");
            }
        }
    }
}
