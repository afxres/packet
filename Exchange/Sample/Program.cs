using Mikodev.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Mikodev.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            var wtr = new PacketWriter()
                .Push("len", 1F)
                .Push("long", 32768L)
                .Push("addr", IPAddress.Parse("192.168.1.1"))
                .Push("endpoint", new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7500))
                .Push("bytes", 0xFF7F4000)
                .PushList("array", new int[] { 1, 2, 3, 4 })
                .PushList("value", new string[] { "hello", "sharp", "net" })
                .Push("inner", new PacketWriter()
                    .Push("one", "inner.one")
                    .Push("two", DateTime.Now)
                    .Push("three", 1.00M)
                    .Push("four", new PacketWriter()
                        .Push("one", Guid.NewGuid())
                    )
                );

            var dwt = wtr as dynamic;
            dwt.temp = "hello";
            dwt.list.one = 1;
            dwt.list.two = new string[] { "A", "Bo", "Can" };
            dwt.list.three = new List<float>() { 1.1F, 2.2F, 3.3F };

            var buf = wtr.GetBytes();
            var res = new PacketReader(buf);
            var dre = res as dynamic;

            Console.WriteLine(res["len"].Pull<int>().ToString("x"));    // float -> int
            Console.WriteLine(res["long"].Pull<short>()); // long -> short
            Console.WriteLine(res["addr"].Pull<IPAddress>());
            Console.WriteLine(res["endpoint"].Pull<IPEndPoint>());
            Console.WriteLine(res["bytes"].PullList().GetView());   // uint -> byte[] RAW Mode
            Console.WriteLine(res["array"].PullList<int>().GetView());
            Console.WriteLine(res["value"].PullList<string>().GetView());
            Console.WriteLine(res.Pull("inner").Pull("one").Pull<string>());
            Console.WriteLine(res["inner"]["two"].Pull<DateTime>());
            Console.WriteLine(res["inner/three"].Pull<decimal>());
            Console.WriteLine(res[@"inner\four\one"].Pull<Guid>());
            Console.WriteLine();

            Console.WriteLine((string)dre.temp);
            Console.WriteLine((int)dre.list.one);
            Console.WriteLine(res["array"].PullList(typeof(int)).GetView());
            Console.WriteLine(((IEnumerable<string>)dre.list.two).GetView());
            Console.WriteLine(((IEnumerable<float>)dre.list.three).GetView());

            var obj = PacketWriter.Serialize(new
            {
                id = 1,
                error = "ok",
                data = new
                {
                    word = "an",
                    count = 10,
                    endpoint = new IPEndPoint(IPAddress.Any, IPEndPoint.MinPort),
                    none = default(string),
                    array = new int[2],
                },
                empty = new { },    // empty node, same as null value or empty array
            });
        }
    }

    static class Modules
    {
        public static string GetView(this IEnumerable lst)
        {
            var stb = new StringBuilder("[");
            var spl = ", ";
            foreach (var i in lst)
                stb.Append(i).Append(spl);
            stb.Length -= spl.Length;
            stb.Append("]");
            return stb.ToString();
        }
    }
}
