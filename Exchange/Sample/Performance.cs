using Mikodev.Network;
using System;

namespace Mikodev.Test
{
    internal static class Performance
    {
        /// <summary>
        /// Performance test
        /// </summary>
        internal static void DoWork()
        {
            const int max = 1 << 20;

            // time cost in release mode
            for (int idx = 0; idx < 10; idx++)
            {
                using (new TraceWatch("Box and unbox")) // < 1 ms
                {
                    for (int i = 0; i < max; i++)
                    {
                        var k = (object)1.1;
                        var r = (double)k;
                    }
                }

                using (new TraceWatch("BitConverter")) // 9 ms
                {
                    for (int i = 0; i < max; i++)
                    {
                        var buf = BitConverter.GetBytes(i);
                        var res = BitConverter.ToInt32(buf, 0);
                    }
                }

                using (new TraceWatch("PacketWriter.Serialize")) // 140 ms, avg
                {
                    for (int i = 0; i < max; i++)
                    {
                        var buf = PacketWriter.Serialize(i).GetBytes();
                        var res = new PacketReader(buf).Pull<int>();
                    }
                }

                using (new TraceWatch("PacketWriter<>")) // 650 ms, avg
                {
                    for (int i = 0; i < max; i++)
                    {
                        var buf = new PacketWriter().Push("some", i).GetBytes();
                        var res = new PacketReader(buf)["some"].Pull<int>();
                    }
                }

                using (new TraceWatch("PacketRawWriter<>")) // 182 ms, avg
                {
                    for (int i = 0; i < max; i++)
                    {
                        var buf = new PacketRawWriter().Push(i).GetBytes();
                        var res = new PacketRawReader(buf).Pull<int>();
                    }
                }

                Console.WriteLine();
            }
        }
    }
}
