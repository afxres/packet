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
            var port = new Random().Next(40000, 50000);
            var server = new UdpClient(port);
            server.BeginReceive(r =>
            {
                var endpoint = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort);
                var bytes = server.EndReceive(r, ref endpoint);
                var reader = new PacketReader(bytes);
                // Deserialize anonymous object
                var value = reader.Deserialize(new
                {
                    id = 0,
                    name = string.Empty,
                    data = new
                    {
                        token = Guid.Empty,
                        timestamp = DateTime.MinValue
                    }
                });

                Console.WriteLine($"message from : {endpoint}");
                Console.WriteLine($"id : {value.id}");
                Console.WriteLine($"name : {value.name}");
                Console.WriteLine($"token : {value.data.token}");
                Console.WriteLine($"timestamp : {value.data.timestamp:u}");
                Console.WriteLine();
            }, null);

            // Serialize an anonymous object.
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
            Performance.DoWork();
        }
    }
}
