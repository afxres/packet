using Mikodev.Network;
using System;

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
        }
    }
}
