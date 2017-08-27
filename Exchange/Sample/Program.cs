using Mikodev.Network;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Mikodev.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            const int port = 47530;
            var server = new UdpClient(port);
            server.BeginReceive(r =>
            {
                var endpoint = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
                var bytes = server.EndReceive(r, ref endpoint);
                var reader = new PacketReader(bytes);

                Console.WriteLine($"message from : {endpoint}");
                Console.WriteLine($"id : {reader["id"].Pull<int>()}");
                Console.WriteLine($"name : {reader["name"].Pull<string>()}");
                Console.WriteLine($"token : {reader["data/token"].Pull<Guid>()}");
                Console.WriteLine($"timestamp : {reader["data/timestamp"].Pull<DateTime>():yyyy-MM-dd HH:mm:ss}");
                Console.WriteLine();
            }, null);

            // serialize an anonymous object.
            var buffer = PacketWriter.Serialize(new
            {
                id = 1,
                name = "mikodev",
                data = new
                {
                    token = Guid.NewGuid(),
                    timestamp = DateTime.Now,
                }
            }).GetBytes();

            var client = new UdpClient();
            client.Send(buffer, buffer.Length, new IPEndPoint(IPAddress.Loopback, port));
            Thread.Sleep(1000);

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
