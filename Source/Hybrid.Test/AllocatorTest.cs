using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Binary;
using System;
using System.Linq;

namespace Mikodev.Testing
{
    [TestClass]
    public class AllocatorTest
    {
        private readonly Cache cache = new Cache();

        private readonly Random random = new Random();

        [TestMethod]
        public void Allocate()
        {
            for (var i = 0; i < 32 * 1024; i += 32)
            {
                var allocator = new Allocator();
                allocator.Allocate(i);
                Assert.IsTrue(allocator.Length == i);
                Assert.IsTrue(allocator.Capacity >= i);
            }
        }

        [TestMethod]
        public void AllocateOverflow()
        {
            AssertExtension.MustFail<OverflowException>(() =>
            {
                var allocator = new Allocator();
                allocator.Allocate(0x4000_0000 + 1);
            });
        }

        [TestMethod]
        public void ConstructorDefault()
        {
            var source = new { id = 2048, name = "some" };
            var converter = cache.GetConverter(source);

            var allocator = new Allocator();
            converter.ToBytes(ref allocator, source);
            var buffer = allocator.ToArray();

            var result = converter.ToValue(buffer);
            Assert.IsFalse(ReferenceEquals(source, result));
            Assert.AreEqual(source, result);
        }

        [TestMethod]
        public void ConstructorArgumentNull()
        {
            AssertExtension.MustFail<ArgumentNullException>(() => new Allocator(null));
        }

        [TestMethod]
        public void ConstructorArray()
        {
            var converter = cache.GetConverter<(int, string)>();

            for (var i = 0; i < 512; i += 4)
            {
                var source = (i, $"index {i}");
                var arrayPool = new byte[i];
                var allocator = new Allocator(arrayPool);
                converter.ToBytes(ref allocator, source);
                var buffer = allocator.ToArray();

                var result = converter.ToValue(buffer);
                Assert.AreEqual(source, result);
            }
        }

        [TestMethod]
        public void AppendBytes()
        {
            for (var i = 0; i < 4096; i += 64)
            {
                var allocator = new Allocator();
                var source = new byte[i];
                random.NextBytes(source);
                allocator.Append(source);
                var buffer = allocator.ToArray();

                Assert.IsTrue(allocator.Length == source.Length);
                Assert.IsTrue(allocator.Length == buffer.Length);
                Assert.IsTrue(source.SequenceEqual(buffer));
            }
        }

        [TestMethod]
        public void AppendChars()
        {
            var source = "今日はいい天気ですね";
            var allocator = new Allocator();
            allocator.Append(source.AsSpan());
            var buffer = allocator.ToArray();
            var result = Converter.Encoding.GetString(buffer);

            Assert.IsTrue(string.Equals(source, result));
        }

        [TestMethod]
        public void AsMemory()
        {
            for (var i = 0; i < 4096; i += 64)
            {
                var allocator = new Allocator();
                var source = new byte[i];
                random.NextBytes(source);
                allocator.Append(source);
                var memory = allocator.AsMemory();

                Assert.IsTrue(allocator.Length == source.Length);
                Assert.IsTrue(allocator.Length == memory.Length);
                Assert.IsTrue(source.SequenceEqual(memory.ToArray()));
            }
        }

        [TestMethod]
        public void AsSpan()
        {
            for (var i = 0; i < 4096; i += 64)
            {
                var allocator = new Allocator();
                var source = new byte[i];
                random.NextBytes(source);
                allocator.Append(source);
                var span = allocator.AsSpan();

                Assert.IsTrue(allocator.Length == source.Length);
                Assert.IsTrue(allocator.Length == span.Length);
                Assert.IsTrue(source.SequenceEqual(span.ToArray()));
            }
        }
    }
}
