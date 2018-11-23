using Mikodev.Network;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Sample
{
    internal static class Program
    {
        internal static async Task Main(string[] args)
        {
            var port = new Random().Next(40000, 50000);
            var server = new UdpClient(port);

            var _ = Task.Run(async () =>
            {
                while (true)
                {
                    var result = await server.ReceiveAsync();
                    var value = PacketConvert.Deserialize(result.Buffer, new
                    {
                        id = default(int),
                        name = default(string),
                        data = new
                        {
                            token = default(Guid),
                            datetime = default(DateTime)
                        }
                    });

                    var message =
                        $"message from : {result.RemoteEndPoint}" + Environment.NewLine +
                        $"id : {value.id}" + $"name : {value.name}" + Environment.NewLine +
                        $"token : {value.data.token}" + Environment.NewLine +
                        $"datetime : {value.data.datetime:u}" + Environment.NewLine;
                    Console.WriteLine(message);
                }
            });

            var buffer = PacketConvert.Serialize(new
            {
                id = 1,
                name = "sharp",
                data = new
                {
                    token = Guid.NewGuid(),
                    datetime = DateTime.Now,
                }
            });

            var client = new UdpClient();
            await client.SendAsync(buffer, buffer.Length, new IPEndPoint(IPAddress.Loopback, port));
            await Task.Delay(Timeout.Infinite);
        }
    }
}
