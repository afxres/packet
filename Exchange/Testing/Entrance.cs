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
            var a = new Dictionary<string, sbyte> { ["a"] = 1, ["beta"] = -1, ["other"] = 127 };
            var b = new Dictionary<byte, string> { [1] = "one", [128] = "many", [255] = "max", [0] = "zero" };
            var c = new Dictionary<string, IPAddress> { ["loopback"] = IPAddress.Loopback, ["ipv6 loopback"] = IPAddress.IPv6Loopback };

            var obj = new { a, b, c };
            var ta = PacketConvert.Serialize(obj);
            var va = PacketConvert.Deserialize(ta, new { a = default(Dictionary<string, sbyte>), b = default(IDictionary<byte, string>), c = default(IDictionary<string, IPAddress>) });

            var wtr = new PacketWriter()
                .SetDictionary("a", a)
                .SetDictionary("b", b)
                .SetDictionary("c", c);
            var tb = wtr.GetBytes();
            var rea = new PacketReader(tb);
            var ra = rea["a"].GetDictionary<string, sbyte>();
            var rb = rea["b"].GetDictionary<byte, string>();
            var rc = rea["c"].GetDictionary<string, IPAddress>();

            Assert.AreEqual(ta.Length, tb.Length);

            ThrowIfNotEqual(a, ra);
            ThrowIfNotEqual(b, rb);
            ThrowIfNotEqual(c, rc);

            ThrowIfNotEqual(a, va.a);
            ThrowIfNotEqual(b, va.b);
            ThrowIfNotEqual(c, va.c);
            return;
        }

        [TestMethod]
        public void DeserializePartial()
        {
            const int margin = 10;
            var obj = new { alpha = 1, beta = "two" };
            var buf = PacketConvert.Serialize(obj);
            var tmp = new byte[buf.Length + margin * 2];
            Buffer.BlockCopy(buf, 0, tmp, margin, buf.Length);

            var res = PacketConvert.Deserialize(tmp, margin, buf.Length, new { alpha = 0, beta = string.Empty });

            Assert.AreEqual(obj.alpha, res.alpha);
            Assert.AreEqual(obj.beta, res.beta);
            return;
        }

        [TestMethod]
        public void HashSet()
        {
            var a = new HashSet<byte>() { 1, 128, 255 };
            var b = new HashSet<string>() { "a", "beta", "candy", "dave" };

            var ta = PacketConvert.Serialize(a);
            var tb = new PacketWriter().SetEnumerable("b", b).GetBytes();

            var ra = PacketConvert.Deserialize<HashSet<byte>>(ta);
            var rea = new PacketReader(tb);
            var rb = rea["b"].GetHashSet<string>();

            ThrowIfNotEqual(a, ra);
            ThrowIfNotEqual(b, rb);
            return;
        }

        [TestMethod]
        public void EmptyCollection()
        {
            var obj = new
            {
                array = new int[0],
                bytes = new byte[0],
                sbytes = new sbyte[0],
                empty = new IPAddress[0],
                numbers = new List<double>(),
                list = new List<string>()
            };
            var buf = PacketConvert.Serialize(obj);
            var res = PacketConvert.Deserialize(buf, obj);

            ThrowIfNotSequenceEqual(obj.array, res.array);
            ThrowIfNotSequenceEqual(obj.list, res.list);
            ThrowIfNotSequenceEqual(obj.numbers, res.numbers);
            ThrowIfNotSequenceEqual(obj.empty, res.empty);
            ThrowIfNotSequenceEqual(obj.bytes, res.bytes);
            ThrowIfNotSequenceEqual(obj.sbytes, res.sbytes);
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
            var rax = rea["a"].GetList<byte>();
            var rbx = rea["b"].GetList<byte>();
            var rc = rea["c"].GetArray<sbyte>();
            var rd = rea["d"].GetArray<sbyte>();
            var rcx = rea["c"].GetList<sbyte>();
            var rdx = rea["d"].GetList<sbyte>();

            ThrowIfNotSequenceEqual(a, ra);
            ThrowIfNotSequenceEqual(b, rb);
            ThrowIfNotSequenceEqual(c, rc);
            ThrowIfNotSequenceEqual(d, rd);
            ThrowIfNotSequenceEqual(a, rax);
            ThrowIfNotSequenceEqual(b, rbx);
            ThrowIfNotSequenceEqual(c, rcx);
            ThrowIfNotSequenceEqual(d, rdx);
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
            var d = new[] { 1.1, 2.2, 3.3, double.NaN };

            wtr.SetEnumerable("byte", a).
                SetEnumerable("string", b).
                SetEnumerable("buffer", c).
                SetEnumerable("floats", d);
            var buf = wtr.GetBytes();

            var rea = new PacketReader(buf);
            var ra = rea["byte"].GetValue<byte[]>();
            var ral = rea["byte"].GetArray<byte>();
            var rb = rea["string"].GetEnumerable<string>();
            var rc = rea["buffer"].GetEnumerable<byte[]>();
            var rd = rea["floats"].GetEnumerable<double>();

            var rax = rea["byte"].GetEnumerable(typeof(byte));
            var rbx = rea["string"].GetEnumerable(typeof(string));

            var raa = rea["byte"].Deserialize<byte[]>();
            var rbl = rea["string"].Deserialize<List<string>>();
            var rdl = rea["floats"].Deserialize<IList<double>>();
            var rbs = rea["string"].Deserialize<HashSet<string>>();
            var rbc = rea["string"].Deserialize<ICollection<string>>();

            Assert.AreEqual(4, rea.Count);
            Assert.AreEqual(4, rea.Keys.Count());
            Assert.AreEqual(c.Count, rc.Count());

            ThrowIfNotSequenceEqual(a, ra);
            ThrowIfNotSequenceEqual(a, ral);
            ThrowIfNotSequenceEqual(b, rb);
            ThrowIfNotSequenceEqual(c.First(), rc.First());
            ThrowIfNotSequenceEqual(d, rd);

            ThrowIfNotEqual(b.ToHashSet(), rbs);
            ThrowIfNotSequenceEqual(a, raa);
            ThrowIfNotSequenceEqual(b, rbl);
            ThrowIfNotSequenceEqual(d, rdl);
            ThrowIfNotSequenceEqual(b, rbc);

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
            var e = new long[] { 0x123456789ABCDEF, 0xFF512429871224, 0x7513A45272CE5B8 };
            var f = new ulong[] { 0xFEDCBA987654321, 0xFB58412A0C8E5 };

            var obj = new
            {
                a,
                b,
                sub = new { c, d, e, f },
            };

            var buf = PacketConvert.Serialize(obj);
            var val = PacketConvert.Deserialize(buf, obj);

            ThrowIfNotSequenceEqual(a, val.a);
            ThrowIfNotSequenceEqual(b, val.b);
            ThrowIfNotSequenceEqual(c, val.sub.c);
            ThrowIfNotSequenceEqual(d, val.sub.d);
            ThrowIfNotSequenceEqual(e, val.sub.e);
            ThrowIfNotSequenceEqual(f, val.sub.f);
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
            catch (PacketException ex) when (ex.ErrorCode == PacketError.InvalidPath) { /* ignore */ }

            try
            {
                var ta = rea.GetItem("b").GetItem("a");
                Assert.Fail();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.InvalidPath) { /* ignore */ }

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
            catch (PacketException ex) when (ex.ErrorCode == PacketError.Overflow) { /* ignore */ }

            try
            {
                var ta = rea.GetItem("invalid");
                Assert.Fail();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.Overflow) { /* ignore */ }
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

        [TestMethod]
        public void DirectConvert()
        {
            var a = 1.1;
            var b = "some";
            var tax = PacketConvert.GetBytes(a);
            var tay = PacketConvert.GetBytes(a, typeof(double));
            var tbx = PacketConvert.GetBytes(b);
            var tby = PacketConvert.GetBytes(b, typeof(string));

            var rax = PacketConvert.GetValue<double>(tax);
            var ray = PacketConvert.GetValue(tay, typeof(double));
            var rbx = PacketConvert.GetValue<string>(tbx);
            var rby = PacketConvert.GetValue(tby, typeof(string));

            Assert.AreEqual(a, rax);
            Assert.AreEqual(a, ray);
            Assert.AreEqual(b, rbx);
            Assert.AreEqual(b, rby);

            var off = new Random().Next(8, 16);
            var ba = new byte[128];
            Buffer.BlockCopy(tax, 0, ba, off, tax.Length);
            var bb = new byte[128];
            Buffer.BlockCopy(tbx, 0, bb, off, tbx.Length);

            var sax = PacketConvert.GetValue<double>(ba, off, tax.Length);
            var say = PacketConvert.GetValue(ba, off, tax.Length, typeof(double));
            var sbx = PacketConvert.GetValue<string>(bb, off, tbx.Length);
            var sby = PacketConvert.GetValue(bb, off, tbx.Length, typeof(string));

            Assert.AreEqual(a, sax);
            Assert.AreEqual(a, say);
            Assert.AreEqual(b, sbx);
            Assert.AreEqual(b, sby);
        }

        [TestMethod]
        public void NotSupported()
        {
            var arr = new int[2, 3];

            try
            {
                var _ = PacketConvert.Serialize(arr);
                Assert.Fail();
            }
            catch (NotSupportedException) { /* ignore */ }
        }

        [TestMethod]
        public void Nest()
        {
            var a = new List<List<int>> { new List<int> { 1, 2 }, new List<int> { 3, 4, 5 } };
            var wtr = PacketWriter.Serialize(a);
            var ta = wtr.GetBytes();

            var b = Enumerable.Range(128, 1).Select(r => new { id = r, text = r.ToString() });
            var ano = PacketWriter.Serialize(b);
            var tb = ano.GetBytes();

            var rea = new PacketReader(ta);
            var ra = rea.Deserialize<IEnumerable<List<int>>>().ToList();
            var rb = PacketConvert.Deserialize(tb, new[] { new { id = 0, text = string.Empty } }.ToHashSet());

            var c = Enumerable.Range(128, 2).ToDictionary(r => r, r => new { id = r, text = r.ToString() });
            var dic = PacketWriter.Serialize(c);
            var tc = dic.GetBytes();
            var rc = PacketConvert.Deserialize(tc, new[] { new { id = 0, text = string.Empty } }.ToDictionary(r => r.id));

            Assert.AreEqual(a.Count, ra.Count);

            for (int i = 0; i < ra.Count; i++)
                ThrowIfNotSequenceEqual(a[i], ra[i]);
            ThrowIfNotEqual(b.ToHashSet(), rb);
            ThrowIfNotEqual(c, rc);
            return;
        }

        [TestMethod]
        public void NestDictionary()
        {
            var a = new[] { new { id = 1, text = "alpha" }, new { id = 2, text = "beta" } }.ToDictionary(r => r.id);
            var b = new[] { new { key = "m", value = 1.1 }, new { key = "n", value = 2.2 } }.ToDictionary(r => r.key);
            var wa = PacketWriter.Serialize(a);
            var wb = PacketWriter.Serialize(b);
            var wtr = new PacketWriter().SetItem("alpha", wa).SetItem("beta", wb);
            var buf = wtr.GetBytes();
            var rea = new PacketReader(buf);
            var obj = rea.Deserialize(new { alpha = a, beta = b });

            ThrowIfNotEqual(a, obj.alpha);
            ThrowIfNotEqual(b, obj.beta);

            var src = new PacketWriter();
            var dyn = (dynamic)src;
            dyn.x = wa;
            dyn.y = wb;
            var tmp = src.GetBytes();
            var res = PacketConvert.Deserialize(tmp, new { x = a, y = b });

            ThrowIfNotEqual(a, res.x);
            ThrowIfNotEqual(b, res.y);

            var c = new[] { new { x = 1, y = "a" } }.ToList();
            var tc = PacketConvert.Serialize(c);
            var ra = PacketConvert.Deserialize(tc, c);

            return;
        }

        [TestMethod]
        public void DuplicateKey()
        {
            var raw = new PacketRawWriter().
                SetValue("one").
                SetValue("text one").
                SetValue("two").
                SetValue("text two").
                SetValue("two").
                SetValue("duplicate");
            var buf = raw.GetBytes();
            var rea = new PacketReader(buf);

            Assert.AreEqual(rea.Count, 0);
        }

        [TestMethod]
        public void InvalidType()
        {
            try
            {
                var list = new HashSet<object> { 1.2F, 3.4D, "5.6M" };
                var result = PacketConvert.Serialize(list);
                Assert.Fail();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.InvalidElementType)
            {
                // ignore
            }

            try
            {
                var empty = new byte[0];
                var reader = new PacketReader(empty);
                var result = reader.Deserialize<Dictionary<object, string>>();
                Assert.Fail();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.InvalidKeyType)
            {
                // ignore
            }

            try
            {
                var empty = new Dictionary<object, string>();
                var writer = PacketWriter.Serialize(empty);
                Assert.Fail();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.InvalidKeyType)
            {
                // ignore
            }

            var array = new[] { 0F, -1.1F, 2.2F };
            var anonymous = new { solution = "Exchange", count = 4 };
            var collection = new Dictionary<int, object>
            {
                [1] = 1,
                [2] = 2L,
                [4] = "four",
                [8] = anonymous,
                [16] = array,
            };

            var buffer = PacketConvert.Serialize(collection);
            var dictionary = PacketConvert.Deserialize<Dictionary<int, object>>(buffer);

            var r1 = ((PacketReader)dictionary[1]).GetValue<int>();
            var r2 = ((PacketReader)dictionary[2]).GetValue<long>();
            var r4 = ((PacketReader)dictionary[4]).GetValue<string>();
            var ro = ((PacketReader)dictionary[8]).Deserialize(anonymous.GetType());
            var ra = ((PacketReader)dictionary[16]).GetArray<float>();
            Assert.AreEqual(collection[1], r1);
            Assert.AreEqual(collection[2], r2);
            Assert.AreEqual(collection[4], r4);

            Assert.AreEqual(anonymous, ro);
            ThrowIfNotSequenceEqual(array, ra);
        }

        [TestMethod]
        public void KeyValueCollection()
        {
            var collection = Enumerable.Range(0, 8).Select(r => new KeyValuePair<int, string>(r, r.ToString()));
            var buffer = PacketConvert.Serialize(collection);
            var reader = new PacketReader(buffer);

            try
            {
                var result = reader.Deserialize<List<KeyValuePair<int, string>>>();
                Assert.Fail();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.InvalidType)
            {
                // ignore
            }

            var origin = collection.ToDictionary(r => r.Key, r => r.Value);
            var dictionary = reader.Deserialize<IDictionary<int, string>>();
            ThrowIfNotEqual(origin, dictionary);
        }
    }
}
