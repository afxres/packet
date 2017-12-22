using Mikodev.Network;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Mikodev.Test
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            //const int port = 47530;
            //var server = new UdpClient(port);
            //server.BeginReceive(r =>
            //{
            //    var endpoint = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
            //    var bytes = server.EndReceive(r, ref endpoint);
            //    var reader = new PacketReader(bytes);

            //    Console.WriteLine($"message from : {endpoint}");
            //    Console.WriteLine($"id : {reader["id"].Pull<int>()}");
            //    Console.WriteLine($"name : {reader["name"].Pull<string>()}");
            //    Console.WriteLine($"token : {reader["data/token"].Pull<Guid>()}");
            //    Console.WriteLine($"timestamp : {reader["data/timestamp"].Pull<DateTime>():u}");
            //    Console.WriteLine();
            //}, null);

            //// serialize an anonymous object.
            //var buffer = PacketWriter.Serialize(new
            //{
            //    id = 1,
            //    name = "sharp",
            //    data = new
            //    {
            //        token = Guid.NewGuid(),
            //        timestamp = DateTime.Now,
            //    }
            //}).GetBytes();

            //var client = new UdpClient();
            //client.Send(buffer, buffer.Length, new IPEndPoint(IPAddress.Loopback, port));
            //Thread.Sleep(1000);

            //// more samples, see unit test.
            //Performance.DoWork();

            var src = new { id = 1, name = "alice", data = new { key = Guid.NewGuid() } };
            var buf = PacketWriter.Serialize(src).GetBytes();
            var rea = new PacketReader(buf);
            var obj = rea.Deserialize(new { id = default(int), name = default(string), data = default(object) });

            Console.WriteLine(src.GetType() == obj.GetType());
            return;
        }
    }
}
