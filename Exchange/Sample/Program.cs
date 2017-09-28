using Mikodev.Network;
using System;
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
                Console.WriteLine($"timestamp : {reader["data/timestamp"].Pull<DateTime>():u}");
                Console.WriteLine();
            }, null);

            // serialize an anonymous object.
            var buffer = PacketWriter.Serialize(new
            {
                id = 1,
                name = "sharp",
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
            Performance();
        }

        /// <summary>
        /// Performance test
        /// </summary>
        static void Performance()
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
