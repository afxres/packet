using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Binary;
using Mikodev.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net;

namespace Mikodev.Testing
{
    [TestClass]
    public class Entrance
    {
        private const int loop = 1024;

        private readonly Cache cache = new Cache();

        private readonly Random random = new Random();

        private void Verify<T>(T value)
        {
            var t1 = cache.ToBytes(value);
            var t2 = PacketConvert.Serialize(value);
            var r1 = cache.ToValue(t2, value);
            var r2 = PacketConvert.Deserialize(t1, value);
            Assert.IsFalse(ReferenceEquals(value, r1));
            Assert.IsFalse(ReferenceEquals(value, r2));
            Assert.AreEqual(value, r1);
            Assert.AreEqual(value, r2);
        }

        [TestMethod]
        public void GuidTest()
        {
            for (int i = 0; i < loop; i++)
            {
                var anonymous = new
                {
                    a = random.Next(),
                    b = Guid.NewGuid(),
                    c = random.Next().ToString(),
                    d = Guid.NewGuid(),
                    e = random.NextDouble(),
                };
                var t = cache.ToBytes(anonymous);
                var r = cache.ToValue(t, anonymous);
                var token = cache.AsToken(t);
                Assert.IsFalse(ReferenceEquals(anonymous, r));
                if (BitConverter.IsLittleEndian == Converter.UseLittleEndian)
                {
                    // native endian
                    var arr = anonymous.b.ToByteArray();
                    var tmp = token["b"].As<byte[]>();
                    CollectionAssert.AreEqual(arr, tmp);
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
                    CollectionAssert.AreEqual(arr, tmp);
                }
                Assert.AreEqual(anonymous, r);
            }
        }

        [TestMethod]
        public void Common()
        {
            for (int i = 0; i < loop; i++)
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
                    number = (decimal)random.NextDouble(),
                    datetime = DateTime.Now,
                    timespan = DateTime.Now - new DateTime(1999, 12, 31),
                };
                Verify(anonymous);
            }
        }

        [TestMethod]
        public void NonGeneric()
        {
            var anonymous = new
            {
                id = 1024,
                text = "string",
            };
            var buffer = cache.ToBytes((object)anonymous);
            var result = cache.ToValue(buffer, anonymous.GetType());

            Assert.IsFalse(ReferenceEquals(anonymous, result));
            Assert.AreEqual(anonymous, result);
        }

        [TestMethod]
        public void PartialBuffer()
        {
            for (int i = 0; i < loop; i++)
            {
                var bytes = new byte[random.Next(4, 64)];
                random.NextBytes(bytes);
                var text = BitConverter.ToString(bytes);

                var anonymous = new
                {
                    byteLength = bytes.Length,
                    text,
                    textLength = text.Length,
                };
                var buffer = cache.ToBytes(anonymous);
                var length = buffer.Length;
                var expand = new byte[buffer.Length + 64];
                var offset = random.Next(0, 64);
                Buffer.BlockCopy(buffer, 0, expand, offset, length);
                var r1 = cache.ToValue(new Block(expand, offset, length), anonymous);
                var r2 = cache.ToValue(new Block(expand, offset, length), anonymous.GetType());
                var r3 = cache.AsToken(new Block(expand, offset, length)).As(anonymous);
                var r4 = cache.AsToken(new Block(expand, offset, length)).As(anonymous.GetType());

                Assert.IsFalse(ReferenceEquals(anonymous, r1));
                Assert.IsFalse(ReferenceEquals(anonymous, r2));
                Assert.IsFalse(ReferenceEquals(anonymous, r3));
                Assert.IsFalse(ReferenceEquals(anonymous, r4));
                Assert.AreEqual(anonymous, r1);
                Assert.AreEqual(anonymous, r2);
                Assert.AreEqual(anonymous, r3);
                Assert.AreEqual(anonymous, r4);
            }
        }

        [TestMethod]
        public void ArrayAndList()
        {
            for (int i = 0; i < loop; i++)
            {
                var anonymous = new
                {
                    int16arr = Enumerable.Range(0, 16).Select(r => (short)random.Next()).ToArray(),
                    int32arr = Enumerable.Range(0, 16).Select(r => random.Next()).ToArray(),
                    int64list = Enumerable.Range(0, 16).Select(r => random.Next() * random.Next()).ToList(),
                    float32arr = Enumerable.Range(0, 32).Select(r => (float)random.Next()).ToArray(),
                    float64list = Enumerable.Range(0, 32).Select(r => random.NextDouble()).ToList(),
                    textarr = Enumerable.Range(0, 8).Select(r => $"{random.Next():x}").ToArray(),
                    textlist = Enumerable.Range(0, 8).Select(r => $"{random.NextDouble()}").ToList(),
                };
                var t1 = cache.ToBytes(anonymous);
                var t2 = PacketConvert.Serialize(anonymous);
                var r1 = cache.ToValue(t2, anonymous);
                var r2 = PacketConvert.Deserialize(t1, anonymous);
                Assert.IsFalse(ReferenceEquals(anonymous, r1));
                Assert.IsFalse(ReferenceEquals(anonymous, r2));

                CollectionAssert.AreEqual(anonymous.int16arr, r1.int16arr);
                CollectionAssert.AreEqual(anonymous.int32arr, r1.int32arr);
                CollectionAssert.AreEqual(anonymous.int64list, r1.int64list);
                CollectionAssert.AreEqual(anonymous.textarr, r1.textarr);
                CollectionAssert.AreEqual(anonymous.textlist, r1.textlist);

                CollectionAssert.AreEqual(anonymous.int16arr, r2.int16arr);
                CollectionAssert.AreEqual(anonymous.int32arr, r2.int32arr);
                CollectionAssert.AreEqual(anonymous.int64list, r2.int64list);
                CollectionAssert.AreEqual(anonymous.textarr, r2.textarr);
                CollectionAssert.AreEqual(anonymous.textlist, r2.textlist);
            }
        }

        [TestMethod]
        public void TupleTest()
        {
            for (int i = 0; i < loop; i++)
            {
                var now = DateTime.Now;
                var anonymous = new
                {
                    t1 = Tuple.Create(random.Next()),
                    t2 = ($"{random.Next()}", random.Next()),
                    t3 = Tuple.Create((short)random.Next(), random.Next(), random.NextDouble()),
                    t4 = (random.Next(), $"{random.NextDouble()}", now, $"{now:u}"),
                    tr = (now.Second, $"{random.Next()}", Guid.NewGuid(), $"{now}", (short)random.Next(), (float)random.NextDouble(), IPAddress.Any, new IPEndPoint(IPAddress.Broadcast, (ushort)random.Next()), now.Millisecond)
                };
                var buffer = cache.ToBytes(anonymous);
                var result = cache.ToValue(buffer, anonymous);
                Assert.IsFalse(ReferenceEquals(anonymous, result));
                Assert.AreEqual(anonymous, result);
            }
        }

        [TestMethod]
        public void DictionaryTest()
        {
            for (int i = 0; i < loop; i++)
            {
                var anonymous = new
                {
                    d1 = Enumerable.Range(0, 16).ToDictionary(r => random.Next(), r => $"{random.Next():x}"),
                    d2 = (IDictionary<string, double>)Enumerable.Range(0, 8).ToDictionary(r => $"{random.Next():x}", r => random.NextDouble()),
                    d3 = Enumerable.Range(0, 32).ToDictionary(r => random.Next(), r => random.NextDouble()),
                };
                var t1 = cache.ToBytes(anonymous);
                var t2 = PacketConvert.Serialize(anonymous);
                var r1 = cache.ToValue(t2, anonymous);
                var r2 = PacketConvert.Deserialize(t1, anonymous);
                Assert.IsFalse(ReferenceEquals(anonymous, r1));
                Assert.IsFalse(ReferenceEquals(anonymous, r2));
                CollectionAssert.AreEqual(anonymous.d1, r1.d1);
                CollectionAssert.AreEqual((ICollection)anonymous.d2, (ICollection)r1.d2);
                CollectionAssert.AreEqual(anonymous.d3, r1.d3);
                CollectionAssert.AreEqual(anonymous.d1, r2.d1);
                CollectionAssert.AreEqual((ICollection)anonymous.d2, (ICollection)r2.d2);
                CollectionAssert.AreEqual(anonymous.d3, r2.d3);
            }
        }

        [TestMethod]
        public void CollectionTest()
        {
            for (int i = 0; i < loop; i++)
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
                var buffer = cache.ToBytes(anonymous);
                var result = cache.ToValue(buffer, anonymous);
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
            var t1 = cache.ToBytes(anonymous);
            var token = cache.AsToken(t1);
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
        public void IPTest()
        {
            var anonymous = new
            {
                name = "sharp",
                address = IPAddress.Parse("192.168.16.32"),
                addressV6 = IPAddress.Parse("fe80::3c03:feef:ec25:e40d"),
                endpoint = new IPEndPoint(IPAddress.Loopback, (ushort)random.Next()),
                emptyAddress = default(IPAddress),
                emptyEndpoint = default(IPEndPoint),
                status = "ok",
            };
            var b1 = cache.ToBytes(anonymous);
            var b2 = PacketConvert.Serialize(anonymous);
            var r1 = cache.ToValue(b2, anonymous);
            var r2 = PacketConvert.Deserialize(b1, anonymous);

            Assert.IsFalse(ReferenceEquals(anonymous, r1));
            Assert.IsFalse(ReferenceEquals(anonymous, r2));
            Assert.AreEqual(anonymous, r1);
            Assert.AreEqual(anonymous, r2);
        }

        [TestMethod]
        public void EmptyTest()
        {
            try
            {
                var bytes = new byte[16];
                var block = new Block(bytes, 8, 0);
                var value = block[0];
                Assert.Fail();
            }
            catch (ArgumentOutOfRangeException) { }

            try
            {
                var token = cache.AsToken(default(Block));
                var texta = token.As<string>();
                var textb = token.As(typeof(string));
                Assert.AreEqual(string.Empty, texta);
                Assert.AreEqual(string.Empty, textb);
                var child = token[string.Empty];
                Assert.Fail();
            }
            catch (KeyNotFoundException) { }
        }

        [TestMethod]
        public void InvalidTest()
        {
            try
            {
                var bytes = new byte[16];
                for (int i = 0; i < bytes.Length; i++)
                    bytes[i] = 0xFF;
                var token = cache.AsToken(bytes);
                var value = token[string.Empty];
                Assert.Fail();
            }
            catch (KeyNotFoundException) { }
        }
    }
}
