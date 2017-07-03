using Mikodev.Network;
using System;
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
                .Push("long", 1L)
                .Push("number", 1.00M)
                .Push("guid", Guid.NewGuid())
                .Push("first", "hello, world!")
                .Push("time", DateTime.Now)
                .Push("addr", IPAddress.Parse("192.168.1.1"))
                .Push("endpoint", new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7500))
                .Push("bytes", 0xFF7F4000)
                .PushList("array", new int[] { 1, 2, 3, 4 })
                .PushList("value", new string[] { "hello", "sharp", "net" })
                .Push("inner", new PacketWriter()
                    .Push("one", "inner.one")
                );

            var dwt = wtr as dynamic;
            dwt.temp = "hello";
            dwt.list.one = 1;

            var buf = wtr.GetBytes();
            var res = new PacketReader(buf);
            var dre = res as dynamic;

            Console.WriteLine(res.Pull<int>("long"));   // long -> int
            Console.WriteLine(res.Pull<decimal>("number"));
            Console.WriteLine(res.Pull<int>("len").ToString("x"));  // float -> int
            Console.WriteLine(res.Pull<Guid>("guid"));
            Console.WriteLine(res.Pull<string>("first"));
            Console.WriteLine(res.Pull<DateTime>("time"));
            Console.WriteLine(res.Pull<IPAddress>("addr"));
            Console.WriteLine(res.Pull<IPEndPoint>("endpoint"));
            Console.WriteLine(res.PullList("bytes").GetView()); // uint -> byte[] RAW Mode
            Console.WriteLine(res.PullList<int>("array").GetView());
            Console.WriteLine(res.PullList<string>("value").GetView());
            Console.WriteLine(res.Pull("inner").Pull<string>("one"));
            Console.WriteLine(res[@"inner/one"].Pull<string>());
            Console.WriteLine(res[@"inner\one"].Pull<string>());
            Console.WriteLine();

            Console.WriteLine((string)dre.temp);
            Console.WriteLine((int)dre.list.one);
            Console.WriteLine((string)dre.inner.one);

            var pre = new PacketReader(Encoding.UTF8.GetBytes("Hello, world!"));
            Console.WriteLine(pre.Read());  // false
        }
    }

    static class Modules
    {
        public static string GetView<T>(this IEnumerable<T> lst)
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
