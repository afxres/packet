using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Binary;
using Mikodev.Network;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;

namespace Mikodev.Testing
{
    [TestClass]
    public class Entrance
    {
        private readonly Cache cache = new Cache();

        private readonly Random random = new Random();

        private void Verify<T>(T value)
        {
            var buffer = cache.Serialize(value);
            var result = cache.Deserialize(buffer, value);
            var legacy = PacketConvert.Deserialize(buffer, value);
            Assert.IsFalse(ReferenceEquals(value, result));
            Assert.IsFalse(ReferenceEquals(value, legacy));
            Assert.AreEqual(value, result);
            Assert.AreEqual(value, legacy);
        }

        [TestMethod]
        public void GuidTest()
        {
            for (int i = 0; i < 8; i++)
            {
                var anonymous = new
                {
                    a = random.Next(),
                    b = Guid.NewGuid(),
                    c = random.Next().ToString(),
                    d = Guid.NewGuid(),
                    e = random.NextDouble(),
                };
                var t = cache.Serialize(anonymous);
                var r = cache.Deserialize(t, anonymous);
                var token = cache.NewToken(t);
                Assert.IsFalse(ReferenceEquals(anonymous, r));
                if (BitConverter.IsLittleEndian == Converter.UseLittleEndian)
                {
                    // native endian
                    var arr = anonymous.b.ToByteArray();
                    var tmp = token["b"].As<byte[]>();
                    Assert.IsTrue(arr.SequenceEqual(tmp));
                }
                if (Converter.UseLittleEndian == false)
                {
                    // always use big endian
                    var hex = anonymous.d.ToString("N");
                    var arr = Enumerable.Range(0, hex.Length)
                         .Where(x => x % 2 == 0)
                         .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                         .ToArray();
                    var tmp = token["d"].As<byte[]>();
                    Assert.IsTrue(arr.SequenceEqual(tmp));
                }
                Assert.AreEqual(anonymous, r);
            }
        }

        [TestMethod]
        public void Common()
        {
            for (int i = 0; i < 16; i++)
            {
                var anonymous = new
                {
                    boolean = (random.Next() & 1) == 0,
                    character = (char)random.Next(),
                    int8 = (sbyte)random.Next(),
                    uint8 = (byte)random.Next(),
                    int16 = (short)random.Next(),
                    uint16 = (ushort)random.Next(),
                    int32 = random.Next(),
                    uint32 = (uint)random.Next(),
                    int64 = (((long)random.Next()) << 32) + random.Next(),
                    uint64 = (ulong)((((long)random.Next()) << 32) + random.Next()),
                    float32 = (float)random.NextDouble(),
                    float64 = random.NextDouble(),
                    datetime = DateTime.Now,
                    timespan = DateTime.Now - new DateTime(1999, 12, 31),
                };
                Verify(anonymous);
            }
        }

        [TestMethod]
        public void TupleTest()
        {
            for (int i = 0; i < 8; i++)
            {
                var anonymous = new
                {
                    t1 = Tuple.Create(random.Next()),
                    t2 = ($"{random.Next()}", random.Next()),
                    t3 = Tuple.Create((short)random.Next(), random.Next(), random.NextDouble()),
                    t4 = (random.Next(), $"{random.NextDouble()}", DateTime.Now, $"{DateTime.Now:u}"),
                };
                var buffer = cache.Serialize(anonymous);
                var result = cache.Deserialize(buffer, anonymous);
                Assert.IsFalse(ReferenceEquals(anonymous, result));
                Assert.AreEqual(anonymous, result);
            }
        }

        [TestMethod]
        public void DictionaryTest()
        {
            for (int i = 0; i < 8; i++)
            {
                var anonymous = new
                {
                    d1 = Enumerable.Range(0, 16).ToDictionary(r => random.Next(), r => $"{random.Next():x}"),
                    d2 = (IDictionary<string, double>)Enumerable.Range(0, 8).ToDictionary(r => $"{random.Next():x}", r => random.NextDouble()),
                    d3 = Enumerable.Range(0, 32).ToDictionary(r => random.Next(), r => random.NextDouble()),
                };
                var buffer = cache.Serialize(anonymous);
                var result = cache.Deserialize(buffer, anonymous);
                var legacy = PacketConvert.Deserialize(buffer, anonymous);
                Assert.IsFalse(ReferenceEquals(anonymous, result));
                Assert.IsFalse(ReferenceEquals(anonymous, legacy));
                Assert.IsTrue(anonymous.d1.SequenceEqual(result.d1));
                Assert.IsTrue(anonymous.d2.SequenceEqual(result.d2));
                Assert.IsTrue(anonymous.d3.SequenceEqual(result.d3));
                Assert.IsTrue(anonymous.d1.SequenceEqual(legacy.d1));
                Assert.IsTrue(anonymous.d2.SequenceEqual(legacy.d2));
                Assert.IsTrue(anonymous.d3.SequenceEqual(legacy.d3));
            }
        }

        [TestMethod]
        public void ArrayAndList()
        {
            for (int i = 0; i < 8; i++)
            {
                var anonymous = new
                {
                    int32arr = Enumerable.Range(0, 16).Select(r => random.Next()).ToArray(),
                    float64list = Enumerable.Range(0, 16).Select(r => random.NextDouble()).ToList(),
                    textarr = Enumerable.Range(0, 8).Select(r => $"{random.Next():x}").ToArray(),
                    textlist = Enumerable.Range(0, 8).Select(r => $"{random.NextDouble()}").ToList(),
                };
                var buffer = cache.Serialize(anonymous);
                var result = cache.Deserialize(buffer, anonymous);
                var legacy = PacketConvert.Deserialize(buffer, anonymous);
                Assert.IsFalse(ReferenceEquals(anonymous, result));
                Assert.IsFalse(ReferenceEquals(anonymous, legacy));

                Assert.IsTrue(anonymous.int32arr.SequenceEqual(result.int32arr));
                Assert.IsTrue(anonymous.float64list.SequenceEqual(result.float64list));
                Assert.IsTrue(anonymous.textarr.SequenceEqual(result.textarr));
                Assert.IsTrue(anonymous.textlist.SequenceEqual(result.textlist));

                Assert.IsTrue(anonymous.int32arr.SequenceEqual(legacy.int32arr));
                Assert.IsTrue(anonymous.float64list.SequenceEqual(legacy.float64list));
                Assert.IsTrue(anonymous.textarr.SequenceEqual(legacy.textarr));
                Assert.IsTrue(anonymous.textlist.SequenceEqual(legacy.textlist));
            }
        }

        [TestMethod]
        public void CollectionTest()
        {
            for (int i = 0; i < 8; i++)
            {
                var anonymous = new
                {
                    set = new HashSet<int>(Enumerable.Range(0, 16).Select(r => random.Next())),
                    iset = (ISet<string>)new HashSet<string>(Enumerable.Range(0, 16).Select(r => random.Next().ToString())),
                    ilist = (IList<int>)Enumerable.Range(0, 16).Select(r => random.Next()).ToList(),
                    collection = (ICollection<string>)Enumerable.Range(0, 16).Select(r => random.Next().ToString()).ToList(),
                    readonlyList = (IReadOnlyList<int>)Enumerable.Range(0, 16).Select(r => random.Next()).ToList(),
                    enumerable = (IEnumerable<int>)Enumerable.Range(0, 16).Select(r => random.Next()).ToList(),
                };
                var buffer = cache.Serialize(anonymous);
                var result = cache.Deserialize(buffer, anonymous);
                var legacy = PacketConvert.Deserialize(buffer, anonymous);
            }
        }

        [TestMethod]
        public void DynamicTest()
        {
            var anonymous = new
            {
                id = random.Next(),
                name = $"{random.Next()}",
                guid = Guid.NewGuid(),
            };
            var t1 = cache.Serialize(anonymous);
            var token = cache.NewToken(t1);
            var d = (dynamic)token;
            var r1 = (Token)d;
            var r2 = (object)d;
            var r3 = (IDynamicMetaObjectProvider)d;
            var id = (int)d.id;
            var name = (string)d.name;
            var guid = (Guid)d.guid;

            Assert.IsTrue(ReferenceEquals(token, r1));
            Assert.IsTrue(ReferenceEquals(token, r2));
            Assert.IsTrue(ReferenceEquals(token, r3));
            Assert.AreEqual(anonymous.id, id);
            Assert.AreEqual(anonymous.name, name);
            Assert.AreEqual(anonymous.guid, guid);
        }

        [TestMethod]
        public void IPAddressTest()
        {
            var anonymous = new
            {
                name = "sharp",
                address = IPAddress.Parse("192.168.16.32"),
                addressV6 = IPAddress.Parse("fe80::3c03:feef:ec25:e40d"),
                endpoint = new IPEndPoint(IPAddress.Loopback, (ushort)random.Next()),
                status = "ok",
            };
            var b1 = cache.Serialize(anonymous);
            var b2 = PacketConvert.Serialize(anonymous);
            var r1 = cache.Deserialize(b2, anonymous);
            var r2 = PacketConvert.Deserialize(b1, anonymous);

            Assert.IsFalse(ReferenceEquals(anonymous, r1));
            Assert.IsFalse(ReferenceEquals(anonymous, r2));
            Assert.AreEqual(anonymous, r1);
            Assert.AreEqual(anonymous, r2);
        }
    }
}
