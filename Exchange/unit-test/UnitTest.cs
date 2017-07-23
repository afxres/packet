using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using static Mikodev.UnitTest.Extensions;

namespace Mikodev.UnitTest
{
    [TestClass]
    public class UnitTest
    {
        [TestMethod]
        public void Empty()
        {
            var wtr = new PacketWriter();
            var buf = wtr.GetBytes();
            Assert.AreEqual(0, buf.Length);

            var res = new PacketReader(buf);
            Assert.AreEqual(0, res.Count);
        }

        [TestMethod]
        public void BasicTypes()
        {
            var a = 0;
            var b = "sample text";
            var c = DateTime.Now;
            var wtr = new PacketWriter();
            wtr.Push("int", a).
                Push("string", b).
                Push("timestamp", c);
            var buf = wtr.GetBytes();

            var rdr = new PacketReader(buf);
            var ra = rdr["int"].Pull<int>();
            var rb = rdr["string"].Pull<string>();
            var rc = rdr["timestamp"].Pull<DateTime>();

            Assert.AreEqual(3, rdr.Count);
            Assert.AreEqual(a, ra);
            Assert.AreEqual(b, rb);
            Assert.AreEqual(c, rc);
        }

        [TestMethod]
        public void Collection()
        {
            var wtr = new PacketWriter();
            var a = new byte[] { 0xDD, 0xCC, 0xBB, 0xAA };
            var b = new int[] { 1, 2 };
            var c = new List<byte[]>()
            {
                a,
                new byte[0],
                new byte[] { 1, 2, 3, 4 }
            };

            wtr.PushList("byte", a).
                PushList("ints", b).
                PushList("buffer", c);
            var buf = wtr.GetBytes();

            var rdr = new PacketReader(buf);
            var ra = rdr["byte"].Pull<byte[]>();
            var rb = rdr["ints"].PullList<int>();
            var rc = rdr["buffer"].PullList<byte[]>();

            Assert.AreEqual(c.Count, rc.Count());

            ThrowIfNotAllEquals(a, ra);
            ThrowIfNotAllEquals(b, rb.ToArray());
            ThrowIfNotAllEquals(c.First(), rc.First());
        }

        [TestMethod]
        public void Dynamic()
        {
            var a = 1234;
            var b = "value";
            var c = new byte[] { 1, 2, 3, 4 };
            var d = new int[] { 1, 2, 3, 4 };
            var e = new List<byte[]>()
            {
                c,
                new byte[0],
            };
            var wtr = new PacketWriter();
            var dwt = (dynamic)wtr;
            dwt.a = a;
            dwt.b = b;
            dwt.c = c;
            dwt.d = d;
            dwt.e = e;

            var buf = wtr.GetBytes();
            var rea = new PacketReader(buf);
            var dre = (dynamic)rea;
            var ra = (int)dre.a;
            var rb = (string)dre.b;
            var rc = (byte[])dre.c;
            var rd = (IEnumerable<int>)dre.d;
            var re = (IEnumerable<byte[]>)dre.e;

            Assert.AreEqual(a, ra);
            Assert.AreEqual(b, rb);
            Assert.AreEqual(e.Count, re.Count());

            ThrowIfNotAllEquals(c, rc);
            ThrowIfNotAllEquals(d, rd.ToArray());
            ThrowIfNotAllEquals(e.First(), re.First());
        }

        [TestMethod]
        public void Serialize()
        {
            var a = 1;
            var b = "value";
            var c = new byte[] { 1, 2, 3, 4 };
            var d = new int[] { 1, 2, 3, 4 };
            var wtr = PacketWriter.Serialize(new
            {
                a = a,
                c = c,
                obj = new
                {
                    b = b,
                    d = d,
                }
            });

            var buf = wtr.GetBytes();
            var rea = new PacketReader(buf);
            var ra = rea["a"].Pull<int>();
            var rb = rea["obj/b"].Pull<string>();
            var rc = rea["c"].PullList<byte>();
            var rd = rea["obj/d"].PullList<int>();

            Assert.AreEqual(a, ra);
            Assert.AreEqual(b, rb);
            ThrowIfNotAllEquals(c, rc.ToArray());
            ThrowIfNotAllEquals(d, rd.ToArray());
        }

        [TestMethod]
        public void Path()
        {
            var a = 1;
            var wtr = new PacketWriter();
            wtr.Push("a", new PacketWriter().Push("a", a));

            var buf = wtr.GetBytes();
            var rea = new PacketReader(buf);

            Assert.AreEqual(a, rea[@"a/a"].Pull<int>());
            Assert.AreEqual(a, rea[@"a\a"].Pull<int>());
            Assert.AreEqual(a, rea["a.a", separator: new string[] { "." }].Pull<int>());
            Assert.AreEqual(a, rea.Pull("a").Pull("a").Pull<int>());

            Assert.AreEqual(null, rea["b/a", true]);
            Assert.AreEqual(null, rea["a/b", true]);
            Assert.AreEqual(null, rea.Pull("b", true));
            Assert.AreEqual(null, rea.Pull("a").Pull("b", true));

            try
            {
                var ta = rea["a/b"];
                throw new ApplicationException();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.PathError) { }

            try
            {
                var ta = rea.Pull("b").Pull("a");
                throw new ApplicationException();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.PathError) { }
        }

        [TestMethod]
        public void Dictionary()
        {
            var a = 1;
            var b = "some";
            var dic = new Dictionary<string, object>
            {
                ["a"] = a,
                ["b"] = b,
                ["c"] = new Dictionary<string, object>()
                {
                    ["a"] = a,
                    ["b"] = b,
                }
            };
            var wtr = PacketWriter.Serialize(dic);
            var buf = wtr.GetBytes();
            var rea = new PacketReader(buf);

            Assert.AreEqual(a, rea["a"].Pull<int>());
            Assert.AreEqual(b, rea["b"].Pull<string>());
            Assert.AreEqual(a, rea["c/a"].Pull<int>());
            Assert.AreEqual(b, rea["c/b"].Pull<string>());
        }
    }
}
