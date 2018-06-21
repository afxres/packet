using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Binary;
using Mikodev.Network;
using System;
using System.Collections.Generic;
using System.Linq;

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
            var sourceValue = Guid.NewGuid();
            var buffer = cache.Serialize(sourceValue);
            var resultValue = cache.Deserialize<Guid>(buffer);
            if (BitConverter.IsLittleEndian == Converter.UseLittleEndian)
            {
                // native endian
                var array = sourceValue.ToByteArray();
                Assert.IsTrue(array.SequenceEqual(buffer));
            }
            if (Converter.UseLittleEndian == false)
            {
                // always use big endian
                var hex = sourceValue.ToString("N");
                var array = Enumerable.Range(0, hex.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                     .ToArray();
                Assert.IsTrue(array.SequenceEqual(buffer));
            }
            Assert.AreEqual(sourceValue, resultValue);
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
    }
}
