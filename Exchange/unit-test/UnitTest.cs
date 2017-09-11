using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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
        public void Basic()
        {
            var a = IPAddress.Loopback;
            var b = new IPEndPoint(IPAddress.Any, IPEndPoint.MaxPort);
            var c = DateTime.Now;
            var d = "sample text";
            var e = 'A';
            var f = 1.1F;
            var g = 1.1D;
            var h = true;
            var i = 1.2345678901234567890123456789M;
            var j = TimeSpan.MaxValue;
            var k = Guid.NewGuid();
            var wtr = new PacketWriter();
            wtr.Push("a", a).
                Push("b", b).
                Push("c", c).
                Push("d", d).
                Push("e", e).
                Push("f", f).
                Push("g", g).
                Push("h", h).
                Push("i", i).
                Push("j", j).
                Push("k", k);
            var buf = wtr.GetBytes();

            var rdr = new PacketReader(buf);
            var ra = rdr["a"].Pull<IPAddress>();
            var rb = rdr["b"].Pull<IPEndPoint>();
            var rc = rdr["c"].Pull<DateTime>();
            var rd = rdr["d"].Pull<string>();
            var re = rdr["e"].Pull<char>();
            var rf = rdr["f"].Pull<float>();
            var rg = rdr["g"].Pull<double>();
            var rh = rdr["h"].Pull<bool>();
            var ri = rdr["i"].Pull<decimal>();
            var rj = rdr["j"].Pull<TimeSpan>();
            var rk = rdr["k"].Pull<Guid>();

            Assert.AreEqual(11, rdr.Count);
            Assert.AreEqual(a, ra);
            Assert.AreEqual(b, rb);
            Assert.AreEqual(c, rc);
            Assert.AreEqual(d, rd);
            Assert.AreEqual(e, re);
            Assert.AreEqual(f, rf);
            Assert.AreEqual(g, rg);
            Assert.AreEqual(h, rh);
            Assert.AreEqual(i, ri);
            Assert.AreEqual(j, rj);
            Assert.AreEqual(k, rk);
        }

        [TestMethod]
        public void Integer()
        {
            var i8 = (sbyte)-1;
            var u8 = (byte)1;
            var i16 = (short)-1;
            var u16 = (ushort)1;
            var i32 = -1;
            var u32 = 1U;
            var i64 = -1L;
            var u64 = 1UL;

            var wtr = new PacketWriter();
            wtr.Push("a", i8).
                Push("b", u8).
                Push("c", i16).
                Push("d", u16).
                Push("e", i32).
                Push("f", u32).
                Push("g", i64).
                Push("h", u64);
            var buf = wtr.GetBytes();

            var rdr = new PacketReader(buf);
            var ri8 = rdr["a"].Pull<sbyte>();
            var ru8 = rdr["b"].Pull<byte>();
            var ri16 = rdr["c"].Pull<short>();
            var ru16 = rdr["d"].Pull<ushort>();
            var ri32 = rdr["e"].Pull<int>();
            var ru32 = rdr["f"].Pull<uint>();
            var ri64 = rdr["g"].Pull<long>();
            var ru64 = rdr["h"].Pull<ulong>();

            Assert.AreEqual(8, rdr.Count);
            Assert.AreEqual(i8, ri8);
            Assert.AreEqual(u8, ru8);
            Assert.AreEqual(i16, ri16);
            Assert.AreEqual(u16, ru16);
            Assert.AreEqual(i32, ri32);
            Assert.AreEqual(u32, ru32);
            Assert.AreEqual(i64, ri64);
            Assert.AreEqual(u64, ru64);
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

            Assert.AreEqual(3, rdr.Count);
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
                Assert.Fail();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.PathError)
            {
                // ignore
            }

            try
            {
                var ta = rea.Pull("b").Pull("a");
                Assert.Fail();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.PathError)
            {
                // ignore
            }
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

            Assert.AreEqual(dic.Count, rea.Count);
            Assert.AreEqual(a, rea["a"].Pull<int>());
            Assert.AreEqual(b, rea["b"].Pull<string>());
            Assert.AreEqual(a, rea["c/a"].Pull<int>());
            Assert.AreEqual(b, rea["c/b"].Pull<string>());
        }

        [TestMethod]
        public void List()
        {
            var a = 1;
            var b = DateTime.Now;
            var wtr = new PacketWriter();
            wtr.Push("a", a);
            wtr.Push("b", b);
            var buf = wtr.GetBytes();
            var rea = new PacketReader(buf);

            Assert.AreEqual(a, rea["a"].PullList<int>().First());
            Assert.AreEqual(b, rea["b"].PullList<DateTime>().First());
        }

        [TestMethod]
        public void Enum()
        {
            var a = DayOfWeek.Wednesday;
            var wtr = new PacketWriter();
            wtr.Push("a", a);
            var buf = wtr.GetBytes();
            var rea = new PacketReader(buf);
            Assert.AreEqual(a, rea["a"].Pull<DayOfWeek>());
        }

        [TestMethod]
        public void SerializeObject()
        {
            var a = 1;
            var b = "Sample text.";
            var c = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            var ta = PacketWriter.Serialize(a).GetBytes();
            var tb = PacketWriter.Serialize(b).GetBytes();
            var tc = PacketWriter.Serialize(c).GetBytes();

            var sa = new PacketReader(ta);
            var sb = new PacketReader(tb);
            var sc = new PacketReader(tc);

            Assert.AreEqual(a, sa.Pull<int>());
            Assert.AreEqual(a, BitConverter.ToInt32(ta, 0));
            Assert.AreEqual(b, sb.Pull<string>());
            Assert.AreEqual(b, Encoding.UTF8.GetString(tb));
            ThrowIfNotAllEquals(c, sc.PullList<int>().ToArray());
        }

        [TestMethod]
        public void Invalid()
        {
            var buf = new byte[1024];
            for (int i = 0; i < buf.Length; i++)
                buf[i] = 0xFF;

            var rea = new PacketReader(buf);
            var ra = rea["invalid", true];
            var rb = rea.Pull("invalid", true);

            Assert.AreEqual(0, rea.Count);
            Assert.AreEqual(0, rea.Keys.Count());
            Assert.AreEqual(null, ra);
            Assert.AreEqual(null, rb);

            try
            {
                var ta = rea["invalid"];
                Assert.Fail();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.Overflow)
            {
                // ignore
            }

            try
            {
                var ta = rea.Pull("invalid");
                Assert.Fail();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.Overflow)
            {
                // ignore
            }
        }
    }
}
