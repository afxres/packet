using Mikodev.Network;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mikodev.Test
{
    internal static class Performance
    {
        /// <summary>
        /// Performance test
        /// </summary>
        internal static void DoWork()
        {
            var dic = new Dictionary<string, List<TimeSpan>>();
            TraceWatch.InstanceDisposed += (tag, span) =>
            {
                if (dic.TryGetValue(tag, out var val))
                    val.Add(span);
                else dic.Add(tag, new List<TimeSpan>() { span });
            };

            const int max = 1 << 20;
            const int loop = 16;

            // time cost in release mode
            for (int idx = 0; idx < loop; idx++)
            {
                using (new TraceWatch("Box and unbox")) // 0.28 ms
                {
                    for (int i = 0; i < max; i++)
                    {
                        var k = (object)1.1;
                        var r = (double)k;
                    }
                }

                using (new TraceWatch("BitConverter")) // 8.40 ms
                {
                    for (int i = 0; i < max; i++)
                    {
                        var buf = BitConverter.GetBytes(i);
                        var res = BitConverter.ToInt32(buf, 0);
                    }
                }

                using (new TraceWatch("Serialize")) // 133.79 ms, avg
                {
                    for (int i = 0; i < max; i++)
                    {
                        var buf = PacketWriter.Serialize(i).GetBytes();
                        var res = new PacketReader(buf).GetValue<int>();
                    }
                }

                using (new TraceWatch("PacketRawWriter<>")) // 153.51 ms, best (unstable)
                {
                    for (int i = 0; i < max; i++)
                    {
                        var buf = new PacketRawWriter().SetValue(i).GetBytes();
                        var res = new PacketRawReader(buf).GetValue<int>();
                    }
                }

                using (new TraceWatch("Serialize (anonymous)")) // 936.41 ms, avg | 919.67 ms (thread static)
                {

                    for (int i = 0; i < max; i++)
                    {
                        var obj = new
                        {
                            data = i,
                        };
                        var buf = PacketWriter.Serialize(obj).GetBytes();
                        var res = new PacketReader(buf)["data"].GetValue<int>();
                    }
                }

                using (new TraceWatch("PacketWriter<>")) // 676.06 ms, avg | 646.73 ms (thread static)
                {
                    for (int i = 0; i < max; i++)
                    {
                        var buf = new PacketWriter().SetValue("some", i).GetBytes();
                        var res = new PacketReader(buf)["some"].GetValue<int>();
                    }
                }
            }

            foreach (var i in dic)
            {
                var key = i.Key;
                var val = i.Value;
                if (val.Count > 5)
                    val.RemoveRange(0, 4);
                var sum = val.Select(r => r.Ticks).Sum();
                var avg = new TimeSpan(sum / val.Count);
                Console.WriteLine($"{key,-24} | total: {new TimeSpan(sum).TotalMilliseconds,10:0.000} ms | avg: {avg.TotalMilliseconds,10:0.000} ms");
            }
        }
    }
}
