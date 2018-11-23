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
        public void Constructor()
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

        [TestMethod]
        public void AsToken()
        {
            var cache = new Cache();
            var source = new { id = 256, name = "sharp" };
            var buffer = cache.ToBytes(source);

            var token = cache.AsToken(buffer);
            Assert.IsTrue(token != null);
            Assert.IsTrue(token["id"].As<int>() == source.id);
            Assert.IsTrue(token["name"].As<string>() == source.name);
        }

        [TestMethod]
        public void GetConverter()
        {
            var cache = new Cache();
            var source = (1, "one");

            var ca = cache.GetConverter(source.GetType());
            var cb = cache.GetConverter<(int, string)>();
            Assert.IsTrue(ReferenceEquals(ca, cb));
        }

        [TestMethod]
        public void GetConverterArgumentNull()
        {
            var cache = new Cache();
            AssertExtension.MustFail<ArgumentNullException>(() => cache.GetConverter(null));
        }

        [TestMethod]
        public void GetConverterAnonymous()
        {
            var cache = new Cache();
            var source = new { id = 256, text = "data" };
            var ca = cache.GetConverter(source.GetType());
            var cb = cache.GetConverter(source);
            Assert.IsTrue(ReferenceEquals(ca, cb));
        }

        [TestMethod]
        public void ToBytesToValue()
        {
            var cache = new Cache();
            var source = Tuple.Create(Guid.NewGuid(), 1.0);

            var buffer = cache.ToBytes(source);
            var result = cache.ToValue<Tuple<Guid, double>>(buffer);
            Assert.IsTrue(Equals(source, result));
            Assert.IsFalse(ReferenceEquals(source, result));
        }

        [TestMethod]
        public void ToBytesToValueObject()
        {
            var cache = new Cache();
            var origin = new { id = "key", data = (Guid.NewGuid(), 1.0) };
            var source = (object)origin;

            var buffer = cache.ToBytes(source);
            var result = cache.ToValue(buffer, source.GetType());
            Assert.IsTrue(Equals(source, result));
            Assert.IsFalse(ReferenceEquals(source, result));
        }

        [TestMethod]
        public void ToBytesToValueAnonymous()
        {
            var cache = new Cache();
            var source = new { id = "item", data = (Guid.NewGuid(), 2.0) };

            var buffer = cache.ToBytes(source);
            var result = cache.ToValue(buffer, source);
            Assert.IsTrue(Equals(source, result));
            Assert.IsFalse(ReferenceEquals(source, result));
        }
    }
}
