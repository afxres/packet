using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Binary;
using System;

namespace Mikodev.Testing
{
    [TestClass]
    public class AllocatorTest
    {
        private const int loop = 4096;
        private readonly Random random = new Random();
        private readonly Cache cache;

        private sealed class Box<T>
        {
            public T Value { get; set; }
        }

        private sealed class BoxBytesConverter : Converter<Box<byte[]>>
        {
            public BoxBytesConverter() : base(0) { }

            public override void ToBytes(Allocator allocator, Box<byte[]> value)
            {
                var source = value.Value;
                var memory = allocator.AllocateMemory(source.Length);
                Assert.AreEqual(memory.Length, source.Length);
                source.AsMemory().CopyTo(memory);
            }

            public override Box<byte[]> ToValue(ReadOnlyMemory<byte> memory)
            {
                return new Box<byte[]> { Value = memory.ToArray() };
            }
        }

        public AllocatorTest()
        {
            cache = new Cache(new[] { new BoxBytesConverter() });
        }

        [TestMethod]
        public void AllocateMemory()
        {
            for (int i = 0; i < loop; i++)
            {
                var length = random.Next(0, 4096);
                var buffer = new byte[length];
                random.NextBytes(buffer);

                var anonymous = new
                {
                    data = new Box<byte[]> { Value = buffer },
                };
                var t = cache.ToBytes(anonymous);
                var r = cache.ToValue(t, anonymous);

                Assert.IsFalse(ReferenceEquals(t, r));
                CollectionAssert.AreEqual(buffer, r.data.Value);
            }
        }
    }
}
