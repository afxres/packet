using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using static Mikodev.Testing.Extensions;

namespace Mikodev.Testing
{
    [TestClass]
    public class Entrance
    {
        [TestMethod]
        public void Empty()
        {
            var wtr = new PacketWriter();
            var buf = wtr.GetBytes();
            Assert.AreEqual(0, buf.Length);

            var res = new PacketReader(buf);
            Assert.AreEqual(0, res.Count);
            Assert.AreEqual(0, res.Keys.Count());
        }

        [TestMethod]
        public void Dictionary()
        {
            var raw = new PacketRawWriter()
                .SetValue("one")
                .SetValue((sbyte)1)
                .SetValue("number")
                .SetValue((sbyte)-1);
            var buf = raw.GetBytes();
            var rea = new PacketReader(buf);
            var res = rea.GetDictionary<string, sbyte>();
            var des = PacketConvert.Deserialize<Dictionary<string, sbyte>>(buf);
            return;
        }

        [TestMethod]
        public void EmptyCollection()
        {
            var obj = new { array = new int[0], list = new List<string>() };
            var buf = PacketConvert.Serialize(obj);
            var res = PacketConvert.Deserialize(buf, obj);

            ThrowIfNotSequenceEqual(obj.array, res.array);
            ThrowIfNotSequenceEqual(obj.list, res.list);
            return;
        }

        [TestMethod]
        public void Basic()
        {
            var a = IPAddress.Loopback;
            var b = new IPEndPoint(IPAddress.Any, IPEndPoint.MaxPort);
            var c = DateTime.Now;
            var d = "sample text";
            var e = '一';
            var f = 1.313131F;
            var g = 1.3131313131313131D;
            var h = true;
            var i = 1.2345678901234567890123456789M;
            var j = DateTime.MaxValue - DateTime.Now;
            var k = Guid.NewGuid();
            var wtr = new PacketWriter();
            wtr.SetValue("a", a).
                SetValue("b", b).
                SetValue("c", c).
                SetValue("d", d).
                SetValue("e", e).
                SetValue("f", f).
                SetValue("g", g).
                SetValue("h", h).
                SetValue("i", i).
                SetValue("j", j).
                SetValue("k", k);
            var buf = wtr.GetBytes();

            var rea = new PacketReader(buf);
            var ra = rea["a"].GetValue<IPAddress>();
            var rb = rea["b"].GetValue<IPEndPoint>();
            var rc = rea["c"].GetValue<DateTime>();
            var rd = rea["d"].GetValue<string>();
            var re = rea["e"].GetValue<char>();
            var rf = rea["f"].GetValue<float>();
            var rg = rea["g"].GetValue<double>();
            var rh = rea["h"].GetValue<bool>();
            var ri = rea["i"].GetValue<decimal>();
            var rj = rea["j"].GetValue<TimeSpan>();
            var rk = rea["k"].GetValue<Guid>();

            Assert.AreEqual(11, rea.Count);
            Assert.AreEqual(11, rea.Keys.Count());
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

            Assert.AreEqual(a, rea["a"].GetValue(typeof(IPAddress)));
            Assert.AreEqual(b, rea["b"].GetValue(typeof(IPEndPoint)));
            Assert.AreEqual(c, rea["c"].GetValue(typeof(DateTime)));
            Assert.AreEqual(j, rea["j"].GetValue(typeof(TimeSpan)));
            Assert.AreEqual(k, rea["k"].GetValue(typeof(Guid)));
        }

        [TestMethod]
        public void Integer()
        {
            var i8 = (sbyte)-13;
            var u8 = (byte)131;
            var i16 = (short)-13131;
            var u16 = (ushort)13131;
            var i32 = -1313131313;
            var u32 = 1313131313U;
            var i64 = -1313131313131313131L;
            var u64 = 1313131313131313131UL;

            var wtr = new PacketWriter();
            wtr.SetValue("a", i8).
                SetValue("b", u8).
                SetValue("c", i16).
                SetValue("d", u16).
                SetValue("e", i32).
                SetValue("f", u32).
                SetValue("g", i64).
                SetValue("h", u64);
            var buf = wtr.GetBytes();

            var rdr = new PacketReader(buf);
            var ri8 = rdr["a"].GetValue<sbyte>();
            var ru8 = rdr["b"].GetValue<byte>();
            var ri16 = rdr["c"].GetValue<short>();
            var ru16 = rdr["d"].GetValue<ushort>();
            var ri32 = rdr["e"].GetValue<int>();
            var ru32 = rdr["f"].GetValue<uint>();
            var ri64 = rdr["g"].GetValue<long>();
            var ru64 = rdr["h"].GetValue<ulong>();

            Assert.AreEqual(8, rdr.Count);
            Assert.AreEqual(8, rdr.Keys.Count());
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
        public void BasicNonGeneric()
        {
            var a = 1234;
            var b = "test";
            var c = new[] { 1.1M, 2.2M };
            var d = new[] { "one", "two", "three" };
            var buf = new PacketWriter().
                SetValue("a", a, typeof(int)).
                SetValue("b", b, typeof(string)).
                SetEnumerable("c", c, typeof(decimal)).
                SetEnumerable("d", d, typeof(string)).
                GetBytes();

            var rea = new PacketReader(buf);
            var ra = rea["a"].GetValue(typeof(int));
            var rb = rea["b"].GetValue(typeof(string));
            var rc = rea["c"].GetEnumerable(typeof(decimal));
            var rd = rea["d"].GetEnumerable(typeof(string));
            Assert.AreEqual(a, ra);
            Assert.AreEqual(b, rb);
            ThrowIfNotSequenceEqual(c, rc.Cast<decimal>());
            ThrowIfNotSequenceEqual(d, rd.Cast<string>());
        }

        [TestMethod]
        public void Array()
        {
            var a = new byte[] { 1, 2, 3, 4 };
            var b = new List<byte> { 0, 2, 4, 8 } as ICollection<byte>;
            var c = new sbyte[] { -1, 0, 1, 2 };
            var d = new List<sbyte> { -2, 0, 2, 4 } as ICollection<sbyte>;

            var wtr = new PacketWriter().
                SetEnumerable("a", a).
                SetEnumerable("b", b).
                SetEnumerable("c", c).
                SetEnumerable("d", d);
            var buf = wtr.GetBytes();

            var rea = new PacketReader(buf);
            var ra = rea["a"].GetArray<byte>();
            var rb = rea["b"].GetArray<byte>();
            var rc = rea["c"].GetArray<sbyte>();
            var rd = rea["d"].GetArray<sbyte>();

            ThrowIfNotSequenceEqual(a, ra);
            ThrowIfNotSequenceEqual(b, rb);
            ThrowIfNotSequenceEqual(c, rc);
            ThrowIfNotSequenceEqual(d, rd);
        }

        [TestMethod]
        public void Collection()
        {
            var wtr = new PacketWriter();
            var a = new byte[] { 11, 22, 33, 44 };
            var b = new[] { "a", "bb", "ccc", "dddd" };
            var c = new List<byte[]>()
            {
                a,
                new byte[0],
                new byte[] { 1, 2, 3, 4 }
            };

            wtr.SetEnumerable("byte", a).
                SetEnumerable("ints", b).
                SetEnumerable("buffer", c);
            var buf = wtr.GetBytes();

            var rdr = new PacketReader(buf);
            var ra = rdr["byte"].GetValue<byte[]>();
            var ral = rdr["byte"].GetArray<byte>();
            var rb = rdr["ints"].GetEnumerable<string>();
            var rc = rdr["buffer"].GetEnumerable<byte[]>();

            var rax = rdr["byte"].GetEnumerable(typeof(byte));
            var rbx = rdr["ints"].GetEnumerable(typeof(string));

            Assert.AreEqual(3, rdr.Count);
            Assert.AreEqual(3, rdr.Keys.Count());
            Assert.AreEqual(c.Count, rc.Count());

            ThrowIfNotSequenceEqual(a, ra);
            ThrowIfNotSequenceEqual(a, ral);
            ThrowIfNotSequenceEqual(b, rb);
            ThrowIfNotSequenceEqual(c.First(), rc.First());

            ThrowIfNotSequenceEqual(a, rax.Cast<byte>());
            ThrowIfNotSequenceEqual(b, rbx.Cast<string>());
        }

        [TestMethod]
        public void Dynamic()
        {
            var a = 1234;
            var b = "value";
            var c = new byte[] { 1, 2, 3, 4 };
            var d = new[] { "a", "bb", "ccc", "dddd" };
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
            dwt.list.a = a;
            dwt.list.b = b;
            dwt.list.c = c;
            dwt.list.d = d;

            var buf = wtr.GetBytes();
            var rdr = new PacketReader(buf);
            var dre = (dynamic)rdr;

            var ra = (int)dre.a;
            var rb = (string)dre.b;
            var rc = (byte[])dre.c;
            var rde = (IEnumerable<string>)dre.d;
            var rdl = (List<string>)dre.d;
            var rda = (string[])dre.d;
            var ree = (IEnumerable<byte[]>)dre.e;
            var rel = (IList<byte[]>)dre.e;
            var rea = (byte[][])dre.e;

            var rla = (int)dre.list.a;
            var rlb = (string)dre.list.b;
            var rlc = (byte[])dre.list.c;
            var rld = (IEnumerable<string>)dre.list.d;

            Assert.AreEqual(a, ra);
            Assert.AreEqual(b, rb);
            Assert.AreEqual(a, rla);
            Assert.AreEqual(b, rlb);
            Assert.AreEqual(e.Count, ree.Count());

            ThrowIfNotSequenceEqual(c, rc);
            ThrowIfNotSequenceEqual(d, rde);
            ThrowIfNotSequenceEqual(d, rdl);
            ThrowIfNotSequenceEqual(d, rda);
            ThrowIfNotSequenceEqual(e.First(), ree.First());
            ThrowIfNotSequenceEqual(e.First(), rel.First());
            ThrowIfNotSequenceEqual(e.First(), rea.First());
            ThrowIfNotSequenceEqual(c, rlc);
            ThrowIfNotSequenceEqual(d, rld);
        }

        [TestMethod]
        public void Serialize()
        {
            var a = 1;
            var b = "value";
            var c = new byte[] { 1, 2, 3, 4 };
            var d = new[] { 1, 2, 3, 4 };
            var e = new PacketRawWriter().SetValue(a).SetValue(b);
            var wtr = PacketWriter.Serialize(new
            {
                a,
                c,
                obj = new { b, d, e, },
                sub = new PacketWriter().SetValue("a", a).SetValue("b", b),
            });

            var buf = wtr.GetBytes();
            var rea = new PacketReader(buf);
            var raw = new PacketRawReader(rea["obj/e"]);
            var ra = rea["a"].GetValue<int>();
            var rb = rea["obj/b"].GetValue<string>();
            var rc = rea["c"].GetEnumerable<byte>();
            var rd = rea["obj/d"].GetEnumerable<int>();

            var ta = raw.GetValue<int>();
            var tb = raw.GetValue<string>();

            var sa = rea["sub/a"].GetValue<int>();
            var sb = rea["sub/b"].GetValue<string>();

            Assert.AreEqual(a, ra);
            Assert.AreEqual(b, rb);
            Assert.AreEqual(a, ta);
            Assert.AreEqual(b, tb);
            Assert.AreEqual(a, sa);
            Assert.AreEqual(b, sb);
            ThrowIfNotSequenceEqual(c, rc);
            ThrowIfNotSequenceEqual(d, rd);
        }

        [TestMethod]
        public void SerializeCollection()
        {
            var a = new byte[] { 1, 3, 5, 7 };
            var b = new sbyte[] { -6, -3, 0, 3, 6, 9 };
            var c = new List<byte> { 192, 128, 64, 0 };
            var d = new List<sbyte> { -14, -7, 0, 7, 14, 21 };

            var obj = new
            {
                a,
                b,
                sub = new { c, d },
            };

            var buf = PacketConvert.Serialize(obj);
            var val = PacketConvert.Deserialize(buf, obj);

            ThrowIfNotSequenceEqual(a, val.a);
            ThrowIfNotSequenceEqual(b, val.b);
            ThrowIfNotSequenceEqual(c, val.sub.c);
            ThrowIfNotSequenceEqual(d, val.sub.d);
        }

        [TestMethod]
        public void Path()
        {
            var a = 1;
            var wtr = new PacketWriter();
            wtr.SetItem("a", new PacketWriter().SetValue("a", a));

            var buf = wtr.GetBytes();
            var rea = new PacketReader(buf);

            Assert.AreEqual(a, rea[@"a/a"].GetValue<int>());
            Assert.AreEqual(a, rea[@"a\a"].GetValue<int>());
            Assert.AreEqual(a, rea.GetItem(new[] { "a", "a" }).GetValue<int>());
            Assert.AreEqual(a, rea.GetItem("a").GetItem("a").GetValue<int>());

            Assert.AreEqual(null, rea["b/a", true]);
            Assert.AreEqual(null, rea["a/b", true]);
            Assert.AreEqual(null, rea.GetItem("b", true));
            Assert.AreEqual(null, rea.GetItem(new[] { "a", "b" }, true));
            Assert.AreEqual(null, rea.GetItem("a").GetItem("b", true));

            try
            {
                var ta = rea["a/b"];
                Assert.Fail();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.PathError) { /* ignore */ }

            try
            {
                var ta = rea.GetItem("b").GetItem("a");
                Assert.Fail();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.PathError) { /* ignore */ }

            try
            {
                var _ = rea.GetItem((string)null);
                Assert.Fail();
            }
            catch (ArgumentNullException) { /* ignore */ }

            try
            {
                var _ = rea.GetItem(new[] { (string)null });
                Assert.Fail();
            }
            catch (ArgumentNullException) { /* ignore */ }
        }

        [TestMethod]
        public void SerializeDictionary()
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
            Assert.AreEqual(dic.Count, rea.Keys.Count());
            Assert.AreEqual(a, rea["a"].GetValue<int>());
            Assert.AreEqual(b, rea["b"].GetValue<string>());
            Assert.AreEqual(a, rea["c/a"].GetValue<int>());
            Assert.AreEqual(b, rea["c/b"].GetValue<string>());
        }

        [TestMethod]
        public void SerializeDictionaryDirectly()
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
            var buf = PacketConvert.Serialize(dic);
            var rea = new PacketReader(buf);

            Assert.AreEqual(dic.Count, rea.Count);
            Assert.AreEqual(dic.Count, rea.Keys.Count());
            Assert.AreEqual(a, rea["a"].GetValue<int>());
            Assert.AreEqual(b, rea["b"].GetValue<string>());
            Assert.AreEqual(a, rea["c/a"].GetValue<int>());
            Assert.AreEqual(b, rea["c/b"].GetValue<string>());
        }

        [TestMethod]
        public void List()
        {
            var a = 1;
            var b = DateTime.Now;
            var wtr = new PacketWriter();
            wtr.SetValue("a", a);
            wtr.SetValue("b", b);
            var buf = wtr.GetBytes();
            var rea = new PacketReader(buf);

            Assert.AreEqual(a, rea["a"].GetEnumerable<int>().First());
            Assert.AreEqual(b, rea["b"].GetEnumerable<DateTime>().First());
        }

        [TestMethod]
        public void Enum()
        {
            var a = DayOfWeek.Wednesday;
            var wtr = new PacketWriter();
            wtr.SetValue("a", a);
            var buf = wtr.GetBytes();
            var rea = new PacketReader(buf);
            Assert.AreEqual(a, rea["a"].GetValue<DayOfWeek>());
        }

        [TestMethod]
        public void SerializeObject()
        {
            var a = 1;
            var b = "Sample text.";
            var c = new[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            var ta = PacketWriter.Serialize(a).GetBytes();
            var tb = PacketWriter.Serialize(b).GetBytes();
            var tc = PacketWriter.Serialize(c).GetBytes();

            var sa = new PacketReader(ta);
            var sb = new PacketReader(tb);
            var sc = new PacketReader(tc);

            Assert.AreEqual(a, sa.GetValue<int>());
            Assert.AreEqual(a, BitConverter.ToInt32(ta, 0));
            Assert.AreEqual(b, sb.GetValue<string>());
            Assert.AreEqual(b, Encoding.UTF8.GetString(tb));
            ThrowIfNotSequenceEqual(c, sc.GetEnumerable<int>());
        }

        [TestMethod]
        public void SerializeObjectDirectly()
        {
            var a = 1;
            var b = "Sample text.";
            var c = new[] { 1, 2, 3, 4, 5, 6, 7, 8 };

            var ta = PacketConvert.Serialize(a);
            var tb = PacketConvert.Serialize(b);
            var tc = PacketConvert.Serialize(c);

            var sa = PacketConvert.GetValue<int>(ta);
            var sb = PacketConvert.GetValue(tb, typeof(string)) as string;
            var sc = new PacketReader(tc);

            Assert.AreEqual(a, sa);
            Assert.AreEqual(a, BitConverter.ToInt32(ta, 0));
            Assert.AreEqual(b, sb);
            Assert.AreEqual(b, Encoding.UTF8.GetString(tb));
            ThrowIfNotSequenceEqual(c, sc.GetEnumerable<int>());
        }

        [TestMethod]
        public void Invalid()
        {
            var buf = new byte[1024];
            for (int i = 0; i < buf.Length; i++)
                buf[i] = 0xFF;

            var rea = new PacketReader(buf);
            var ra = rea["invalid", true];
            var rb = rea.GetItem("invalid", true);

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
                var ta = rea.GetItem("invalid");
                Assert.Fail();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.Overflow)
            {
                // ignore
            }
        }

        [TestMethod]
        public void RawArray()
        {
            var src = new[] { 1, 2, 3, 4, 5, 6, 7, 8 };
            var arr = PacketWriter.Serialize(src).GetBytes();
            var rea = new PacketRawReader(new PacketReader(arr));
            var res = new List<int>();
            while (rea.Any)
                res.Add(rea.GetValue<int>());

            ThrowIfNotSequenceEqual(src, res);
        }

        [TestMethod]
        public void RawObject()
        {
            var a = "Hello, world!";
            var b = 0xFF;
            var res = new PacketRawWriter().SetValue(a).SetValue(b).GetBytes();

            var rea = new PacketRawReader(res, 0, res.Length);
            var ra = rea.GetValue<string>();
            var rb = rea.GetValue<int>();
            Assert.AreEqual(a, ra);
            Assert.AreEqual(b, rb);

            rea.Reset();
            Assert.AreEqual(a, rea.GetValue(typeof(string)));
            Assert.AreEqual(b, rea.GetValue(typeof(int)));
        }

        [TestMethod]
        public void RawDynamic()
        {
            var a = "Hello, world!";
            var b = 0xFF;
            var raw = new PacketRawWriter().SetValue(a, typeof(string)).SetValue(b, typeof(int));
            var wtr = new PacketWriter() as dynamic;
            wtr.a = a;
            wtr.b = b;
            wtr.raw = raw;
            var res = wtr.GetBytes();

            var src = new PacketReader(res) as dynamic;
            var rea = new PacketRawReader(src.raw); // PacketReader or byte[]? Both of them can work
            var ra = rea.GetValue<string>();
            var rb = rea.GetValue<int>();

            Assert.AreEqual(a, (string)src.a);
            Assert.AreEqual(b, (int)src.b);
            Assert.AreEqual(a, ra);
            Assert.AreEqual(b, rb);
        }

        [TestMethod]
        public void MixMode()
        {
            var a = 1234;
            var b = "What the ...";
            var wtr = new PacketWriter().
                SetValue("a", a).
                SetValue("b", b).
                SetItem("c", new PacketRawWriter().
                    SetValue(a).
                    SetValue(b));
            var buf = wtr.GetBytes();
            var rea = new PacketReader(buf);
            var ra = rea["a"].GetValue<int>();
            var rb = rea["b"].GetValue<string>();
            var rc = new PacketRawReader(rea["c"]);
            var rca = rc.GetValue<int>();
            var rcb = rc.GetValue<string>();

            Assert.AreEqual(a, ra);
            Assert.AreEqual(b, rb);
            Assert.AreEqual(rca, a);
            Assert.AreEqual(rcb, b);
            Assert.AreEqual(false, rc.Any);
        }
    }
}
