using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using static Mikodev.Tests.Extensions;

namespace Mikodev.Tests
{
    internal enum TestStatus : int { None, Success, Error, }

    internal class TestRef { }

    internal class TestConverter : PacketConverter<TestRef>
    {
        internal static readonly string _BytesErr = Guid.NewGuid().ToString();

        internal static readonly string _ValueErr = Guid.NewGuid().ToString();

        public TestConverter() : base(0) { }

        public override byte[] GetBytes(TestRef value) => throw new Exception(_BytesErr);

        public override TestRef GetValue(byte[] buffer, int offset, int length) => throw new OutOfMemoryException(_ValueErr);
    }

    internal class TestBadConverter : PacketConverter<TestRef>
    {
        public TestBadConverter() : base(4) { }

        public override byte[] GetBytes(TestRef value)
        {
            return null;
        }

        public override TestRef GetValue(byte[] buffer, int offset, int length)
        {
            return null;
        }
    }

    internal class TestTwo
    {
        public int One { get; set; }

        public int Two { get; set; }

        public override bool Equals(object obj)
        {
            return obj is TestTwo two &&
                   this.One == two.One &&
                   this.Two == two.Two;
        }

        public override int GetHashCode()
        {
            var hashCode = -661386576;
            hashCode = hashCode * -1521134295 + this.One.GetHashCode();
            hashCode = hashCode * -1521134295 + this.Two.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return $"{nameof(TestTwo)} one: {this.One}, two: {this.Two}";
        }
    }

    internal class TestTwoConverter : PacketConverter<TestTwo>
    {
        public TestTwoConverter() : base(sizeof(int) * 2) { }

        public override byte[] GetBytes(TestTwo value)
        {
            var two = value;
            var buf = new byte[sizeof(int) * 2];
            var a = BitConverter.GetBytes(two.One);
            var b = BitConverter.GetBytes(two.Two);
            Buffer.BlockCopy(a, 0, buf, 0, sizeof(int));
            Buffer.BlockCopy(b, 0, buf, sizeof(int), sizeof(int));
            return buf;
        }

        public override TestTwo GetValue(byte[] buffer, int offset, int length)
        {
            if (length < (sizeof(int) * 2))
                throw new ArgumentOutOfRangeException(nameof(length));
            var a = BitConverter.ToInt32(buffer, offset);
            var b = BitConverter.ToInt32(buffer, offset + sizeof(int));
            var two = new TestTwo { One = a, Two = b };
            return two;
        }
    }

    internal class TestBox
    {
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            return obj is TestBox box ? this.Name.Equals(box.Name) : false;
        }

        public override int GetHashCode()
        {
            return this.Name?.GetHashCode() ?? 0;
        }

        public override string ToString()
        {
            return $"{nameof(TestBox)} {this.Name}";
        }
    }

    internal class TestBoxConverter : PacketConverter<TestBox>
    {
        public TestBoxConverter() : base(0) { }

        public override byte[] GetBytes(TestBox value)
        {
            return Encoding.UTF8.GetBytes(value.Name);
        }

        public override TestBox GetValue(byte[] buffer, int offset, int length)
        {
            return new TestBox { Name = Encoding.UTF8.GetString(buffer, offset, length) };
        }
    }

    internal class TestPerson
    {
        public int Age { get; set; }

        public string Name { get; set; }
    }

    internal class TestReadOnly
    {
        public int Number { get; }

        public string Text { get; }

        public TestReadOnly(int num, string text)
        {
            this.Number = num;
            this.Text = text;
        }
    }

    internal class TestWriteOnly
    {
        public string Name => nameof(TestWriteOnly);

        public string Value { set { } }
    }

    internal struct TestValue
    {
        public int One { get; set; }

        public string Two { get; set; }
    }

    internal class TestIndex : IEquatable<TestIndex>
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            return this.Equals(obj as TestIndex);
        }

        public bool Equals(TestIndex other)
        {
            return other != null &&
                   this.Id == other.Id &&
                   this.Name == other.Name;
        }

        public override int GetHashCode()
        {
            var hashCode = -1919740922;
            hashCode = hashCode * -1521134295 + this.Id.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.Name);
            return hashCode;
        }

        public override string ToString()
        {
            return $"{nameof(TestIndex)} id: {this.Id}, name: {this.Name}";
        }
    }

    internal class TestAddOnlyList<T> : IEnumerable<T>
    {
        private readonly List<T> list = new List<T>();

        public void Add(T item) => this.list.Add(item);

        public IEnumerator<T> GetEnumerator() => this.list.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.list.GetEnumerator();
    }

    [TestClass]
    public class Logical
    {
        [TestMethod]
        public void Rethrow()
        {
            var val = new TestRef();
            var con = new Dictionary<Type, PacketConverter> { [typeof(TestRef)] = new TestConverter() };

            try
            {
                var buf = new PacketRawWriter(con).SetValue(val);
                Assert.Fail();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.ConversionError && ex.InnerException.Message == TestConverter._BytesErr)
            {
                // ignore
            }

            try
            {
                var res = new PacketRawReader(new byte[4], con).GetValue<TestRef>();
            }
            catch (OutOfMemoryException ex) when (ex.Message == TestConverter._ValueErr)
            {
                // ignore
            }
        }

        [TestMethod]
        public void MultiThread()
        {
            var lst = new List<Task>();
            const int _max = 1 << 16;
            const int _tasks = 16;
            for (var i = 0; i < _tasks; i++)
            {
                var idx = i;
                lst.Add(new Task(() =>
                {
                    for (var k = 0; k < _max; k++)
                    {
                        var obj = new
                        {
                            task = idx,
                            data = k,
                        };
                        var pkt = PacketWriter.Serialize(obj);
                        var buf = pkt.GetBytes();
                        var rea = new PacketReader(buf);
                        Assert.AreEqual(idx, rea["task"].GetValue<int>());
                        Assert.AreEqual(k, rea["data"].GetValue<int>());
                    }
                }));
            }

            lst.ForEach(r => r.Start());
            Task.WaitAll(lst.ToArray());
        }

        [TestMethod]
        public void Collection()
        {
            var arr = Enumerable.Range(0, 8).Select(r => new TestIndex { Id = r, Name = r.ToString() }).ToArray();
            var buf = PacketConvert.Serialize(arr);
            var rs = PacketConvert.Deserialize<PacketReader[]>(buf);
            var rr = PacketConvert.Deserialize<PacketRawReader[]>(buf);
            var ra = PacketConvert.Deserialize<TestIndex[]>(buf);
            var rl = PacketConvert.Deserialize<List<TestIndex>>(buf);
            var ri = PacketConvert.Deserialize<IList<TestIndex>>(buf);
            var rc = PacketConvert.Deserialize<ICollection<TestIndex>>(buf);

            ThrowIfNotSequenceEqual(arr, ra);
            ThrowIfNotSequenceEqual(arr, rl);
            ThrowIfNotSequenceEqual(arr, ri);
            ThrowIfNotSequenceEqual(arr, rc);
            return;
        }

        [TestMethod]
        public void DeserializeAnonymous()
        {
            var a = new
            {
                id = 0,
                name = "Bob",
                data = new
                {
                    array = new[] { 1, 2, 3, 4 },
                    buffer = new byte[] { 1, 2, 3, 4 },
                }
            };
            var pkt = PacketWriter.Serialize(a);
            var buf = pkt.GetBytes();
            var rea = new PacketReader(buf);
            var val = rea.Deserialize(new { id = 0, name = string.Empty, data = new { array = default(int[]), buffer = default(byte[]) } });

            Assert.AreEqual(a.id, val.id);
            Assert.AreEqual(a.name, val.name);

            ThrowIfNotSequenceEqual(a.data.array, val.data.array);
            ThrowIfNotSequenceEqual(a.data.buffer, val.data.buffer);
        }

        [TestMethod]
        public void Deserialize()
        {
            var p = new TestPerson() { Age = 20, Name = "Bob" };
            var pkt = PacketWriter.Serialize(p);
            var buf = pkt.GetBytes();
            var rea = new PacketReader(buf);
            var val = rea.Deserialize<TestPerson>();

            Assert.AreEqual(p.Age, val.Age);
            Assert.AreEqual(p.Name, val.Name);
        }

        [TestMethod]
        public void DeserializeOffset()
        {
            var obj = new { id = 1.1, text = "what the" };
            var ta = PacketConvert.Serialize(obj);
            var buf = new byte[ta.Length + 128];
            var off = new Random().Next(8, 32);
            Buffer.BlockCopy(ta, 0, buf, off, ta.Length);

            var ra = PacketConvert.Deserialize(ta, obj.GetType());
            var rb = PacketConvert.Deserialize(buf, off, ta.Length, obj.GetType());

            void AreEqual(object a, object b)
            {
                var x = a.Cast(obj);
                var y = b.Cast(obj);
                if (x.id == y.id && x.text == y.text)
                    return;
                throw new ApplicationException();
            }

            AreEqual(obj, ra);
            AreEqual(obj, rb);

            var str = "world";
            var tb = PacketConvert.GetBytes(str);
            var tc = new byte[tb.Length + 128];
            Buffer.BlockCopy(tb, 0, tc, off, tb.Length);
            var rc = PacketConvert.Deserialize<string>(tc, off, tb.Length);
            Assert.AreEqual(str, rc);
        }

        [TestMethod]
        public void DeserializeReadOnly()
        {
            var a = new TestReadOnly(10, "read");
            var pkt = PacketWriter.Serialize(a);
            var buf = pkt.GetBytes();
            var rea = new PacketReader(buf);

            try
            {
                var val = rea.Deserialize<TestReadOnly>();
                Assert.Fail();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.InvalidType)
            {
                // ignore
            }
        }

        [TestMethod]
        public void DeserializeWriteOnly()
        {
            var a = new TestWriteOnly();
            var pkt = PacketWriter.Serialize(a);
            var buf = pkt.GetBytes();
            var rea = new PacketReader(buf);
            var obj = rea.Deserialize<TestWriteOnly>();
            var val = rea.GetItem(nameof(TestWriteOnly.Value), true);

            Assert.AreEqual(val, null);
        }

        [TestMethod]
        public void DeserializeStructural()
        {
            var v = new TestValue { One = 1, Two = "Two" };
            var pkt = PacketWriter.Serialize(v);
            var buf = pkt.GetBytes();
            var rea = new PacketReader(buf);
            var obj = rea.Deserialize<TestValue>();

            Assert.AreEqual(obj.One, 1);
            Assert.AreEqual(obj.Two, "Two");
        }

        [TestMethod]
        public void ConversionMismatch()
        {
            var cvt = new Dictionary<Type, PacketConverter>() { [typeof(TestRef)] = new TestBadConverter() };

            try
            {
                var buf = PacketConvert.Serialize(new TestRef(), cvt);
                Assert.Fail();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.ConversionMismatch)
            {
                // ignore
            }
        }

        [TestMethod]
        public void CustomEnum()
        {
            var obj = new
            {
                err = TestStatus.Success,
                iep = new IPEndPoint(IPAddress.Loopback, IPEndPoint.MaxPort),
            };
            var wtr = PacketWriter.Serialize(obj);
            var buf = wtr.GetBytes();
            var rea = new PacketReader(buf);
            var res = rea.Deserialize(obj);

            Assert.AreEqual(obj.err, res.err);
            Assert.AreEqual(obj.iep, res.iep);
        }

        [TestMethod]
        public void ISet()
        {
            var random = new Random();
            var set = new HashSet<int>(Enumerable.Range(0, 16).Select(_ => random.Next()));
            var buf = PacketConvert.Serialize(set);
            var res = PacketConvert.Deserialize<HashSet<int>>(buf);
            Assert.AreEqual(set.Count, res.Count);
            Assert.IsTrue(set.SetEquals(res));

            var obj = new { set = (ISet<string>)new HashSet<string>(Enumerable.Range(0, 16).Select(_ => random.Next().ToString())) };
            var ta = PacketConvert.Serialize(obj);
            var rea = new PacketReader(ta);
            var ra = rea["set"].Deserialize<ISet<string>>();
            ThrowIfNotEqual(obj.set, ra);
        }

        [TestMethod]
        public void CustomConverter()
        {
            var con = new Dictionary<Type, PacketConverter>()
            {
                [typeof(TestTwo)] = new TestTwoConverter(),
                [typeof(TestBox)] = new TestBoxConverter(),
            };
            var obj = new
            {
                a = Enumerable.Range(0, 4).Select(r => new TestTwo { One = r, Two = r * 2 }),
                b = Enumerable.Range(0, 8).ToDictionary(r => r.ToString(), r => new TestTwo { One = r * 2, Two = r * 4 }),
                x = Enumerable.Range(0, 2)
                    .Select(t =>
                        Enumerable.Range(0, 4).Select(r =>
                            new TestBox { Name = $"{t}:{r}" }))
                    .ToList(),
            };
            var buf = PacketConvert.Serialize(obj, con);
            var rea = new PacketReader(buf, con);
            var itr = rea["a"].GetEnumerable<TestTwo>();

            var ra = itr.ToList();
            var rb = rea["b"].GetDictionary<string, TestTwo>();
            var rx = rea["x"].Deserialize<TestBox[][]>();

            var od = new[] { new TestBox { Name = "one" }, new TestBox { Name = "Loooooooooooooong name!" }, new TestBox { Name = "what?" } };
            var td = PacketConvert.Serialize(od, con);
            var rd = PacketConvert.Deserialize<IEnumerable<TestBox>>(td, con);
            var rdx = PacketConvert.Deserialize<TestBox[]>(td, con);
            var rdy = PacketConvert.Deserialize<List<TestBox>>(td, con);

            var rax = rea["a"].Deserialize<TestTwo[]>();
            var ray = rea["a"].Deserialize<List<TestTwo>>();

            ThrowIfNotSequenceEqual(od, rd);
            ThrowIfNotSequenceEqual(od, rdx);
            ThrowIfNotSequenceEqual(od, rdy);
            ThrowIfNotSequenceEqual(obj.a, rax);
            ThrowIfNotSequenceEqual(obj.a, ray);
            return;
        }

        [TestMethod]
        public void CollectionWithAddFunction()
        {
            var random = new Random();
            TestAddOnlyList<T> ToAddOnlyList<T>(IEnumerable<T> enumerable)
            {
                var list = new TestAddOnlyList<T>();
                foreach (var i in enumerable)
                    list.Add(i);
                return list;
            }
            var xa = Enumerable.Range(0, 4).Select(r => random.Next()).ToArray();
            var la = ToAddOnlyList(xa);
            var ta = PacketConvert.Serialize(la);
            var ra = PacketConvert.Deserialize<TestAddOnlyList<int>>(ta);
            ThrowIfNotSequenceEqual(ra, xa);

            var xb = Enumerable.Range(0, 4).Select(r => new { id = random.Next(), text = random.Next().ToString("x") }).ToArray();
            var lb = ToAddOnlyList(xb);
            var tb = PacketConvert.Serialize(lb);
            var rb = PacketConvert.Deserialize(tb, lb);
            ThrowIfNotSequenceEqual(rb, xb);
            return;
        }
    }
}
