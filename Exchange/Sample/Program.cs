using Mikodev.Network;
using System;
using System.Diagnostics;

namespace Mikodev.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            // serialize an anonymous object to byte array.
            var buffer = PacketWriter.Serialize(new
            {
                id = 1,
                name = "mikodev",
                data = new
                {
                    token = Guid.NewGuid(),
                    last_time = DateTime.Now,
                }
            }).GetBytes();

            var reader = new PacketReader(buffer);

            Console.WriteLine($"my id : {reader["id"].Pull<int>()}");
            Console.WriteLine($"my name : {reader["name"].Pull<string>()}");
            Console.WriteLine($"my token : {reader["data/token"].Pull<Guid>()}");
            Console.WriteLine($"my last online time : {reader["data/last_time"].Pull<DateTime>():yyyy-MM-dd HH:mm:ss}");

            // more samples, see unit test.

            var times = 1 << 20;

            for (int i = 0; i < 1 << 26; i++)
            {
                var t = i * i;
            }

            // 12 ~ 13 ms Debug
            using (var t = new TraceWatch())
            {
                for (var i = 0; i < times; i++)
                {
                    var buf = BitConverter.GetBytes(i);
                    var res = BitConverter.ToInt32(buf, 0);
                }
            }

            // 230 ~ 240 ms Debug
            using (var t = new TraceWatch())
            {
                for (var i = 0; i < times; i++)
                {
                    var buf = PacketWriter.Serialize(i).GetBytes();
                    var res = new PacketReader(buf).Pull<int>();
                }
            }
        }
    }

    class TraceWatch : IDisposable
    {
        private Stopwatch _watch = new Stopwatch();

        public TraceWatch()
        {
            _watch.Start();
        }

        public void Dispose()
        {
            _watch.Stop();
            Console.WriteLine($"[{_watch.ElapsedMilliseconds} ms]");
        }
    }
}
