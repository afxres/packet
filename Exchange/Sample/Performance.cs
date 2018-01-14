﻿using Mikodev.Network;
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
            const int loop = 10;

            var ano = new
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

            var tmp = PacketConvert.Serialize(ano);

            // release mode
            for (int idx = 0; idx < loop; idx++)
            {
                using (new TraceWatch("BitConverter")) // 8.30 ms
                {
                    for (int i = 0; i < max; i++)
                    {
                        var buf = BitConverter.GetBytes(i);
                        var res = BitConverter.ToInt32(buf, 0);
                    }
                }

                using (new TraceWatch("PacketWriter")) // 150.62 ms, avg
                {
                    for (int i = 0; i < max; i++)
                    {
                        var buf = PacketWriter.Serialize(i).GetBytes();
                        var res = new PacketReader(buf).GetValue<int>();
                    }
                }

                using (new TraceWatch("PacketRawWriter<>")) // 189.93 ms, avg
                {
                    for (int i = 0; i < max; i++)
                    {
                        var buf = new PacketRawWriter().SetValue(i).GetBytes();
                        var res = new PacketRawReader(buf).GetValue<int>();
                    }
                }

                using (new TraceWatch("PacketWriter<>")) // 684.76 ms, avg
                {
                    for (int i = 0; i < max; i++)
                    {
                        var buf = new PacketWriter().SetValue("some", i).GetBytes();
                        var res = new PacketReader(buf)["some"].GetValue<int>();
                    }
                }

                using (new TraceWatch("PacketWriter<> Set")) // 2361.77 ms, avg
                {
                    for (int i = 0; i < max; i++)
                    {
                        var wtr = new PacketWriter().
                            SetValue(nameof(ano.num), ano.num).
                            SetValue(nameof(ano.str), ano.str).
                            SetEnumerable(nameof(ano.arr), ano.arr).
                            SetItem(nameof(ano.sub), new PacketWriter().
                                SetValue(nameof(ano.sub.sum), ano.sub.sum).
                                SetEnumerable(nameof(ano.sub.lst), ano.sub.lst));
                        var buf = wtr.GetBytes();
                    }
                }

                using (new TraceWatch("Serialize (anonymous)")) // 3811.01 ms, avg
                {
                    for (int i = 0; i < max; i++)
                    {
                        var _ = PacketConvert.Serialize(ano);
                    }
                }

                using (new TraceWatch("Deserialize (anonymous)")) // 2805.59 ms, avg
                {
                    for (int i = 0; i < max; i++)
                    {
                        var _ = PacketConvert.Deserialize(tmp, ano);
                    }
                }
            }

            foreach (var i in dic)
            {
                var key = i.Key;
                var val = i.Value;
                if (val.Count > 6)
                    val.RemoveRange(0, 4);
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
