using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Binary;
using System;
using System.Linq;

namespace Mikodev.Testing
{
    [TestClass]
    public class CacheTest
    {
        private readonly Random random = new Random();

        [TestMethod]
        public void ConstructTest()
        {
            void validate(Cache cache)
            {
                var anonymous = new { int32 = random.Next(), float64 = random.NextDouble() };
                var buffer = cache.ToBytes(anonymous);
                var result = cache.ToValue(buffer, anonymous);

                Assert.IsFalse(ReferenceEquals(anonymous, result));
                Assert.AreEqual(anonymous, result);

            }

            validate(new Cache());
            validate(new Cache(null));
            validate(new Cache(Enumerable.Empty<Converter>()));
            validate(new Cache(new Converter[] { null }));
        }
    }
}
