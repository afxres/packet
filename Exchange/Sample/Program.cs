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
            // basic
            var pkt = new PacketWriter()
                .Push("len", 1F)
                .Push("long", 1L)
                .Push("number", 1.00M)
                .Push("guid", Guid.NewGuid())
                .Push("first", "hello, world!")
                .Push("time", DateTime.Now)
                .Push("addr", IPAddress.Parse("192.168.1.1"))
                .Push("endpoint", new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7500))
                .Push("bytes", 0xFF7F4000)
                .Push("temp", "temp value")
                .PushList("array", new int[] { 1, 2, 3, 4 })
                .PushList("value", new string[] { "hello", "sharp", "net" })
                .Push("inner", new PacketWriter()
                    .Push("one", "inner.one"))
                .GetBytes();

            var rdr = new PacketReader(pkt);
            Console.WriteLine(rdr.Pull<int>("long"));   // long -> int
            Console.WriteLine(rdr.Pull<decimal>("number"));
            Console.WriteLine(rdr.Pull<int>("len").ToString("x"));  // float -> int
            Console.WriteLine(rdr.Pull<Guid>("guid"));
            Console.WriteLine(rdr.Pull<string>("first"));
            Console.WriteLine(rdr.Pull<DateTime>("time"));
            Console.WriteLine(rdr.Pull<IPAddress>("addr"));
            Console.WriteLine(rdr.Pull<IPEndPoint>("endpoint"));
            Console.WriteLine(rdr.PullList("bytes").GetView()); // uint -> byte[] RAW Mode
            Console.WriteLine(rdr.PullList<int>("array").GetView());
            Console.WriteLine(rdr.PullList<string>("value").GetView());
            Console.WriteLine(rdr.Pull("inner").Pull<string>("one"));
            Console.WriteLine();

            // dynamic
            var src = new PacketWriter() as dynamic;
            src.one.value = new byte[] { 0xFF, 0xFF, 0x00, 0x00 };
            src.tmp = "temp";
            var dyn = new PacketReader(src.GetBytes()) as dynamic;
            Console.WriteLine((int)dyn.one.value); // byte[] -> int
            Console.WriteLine((string)dyn.tmp);
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
