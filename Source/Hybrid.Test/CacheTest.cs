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
            void validate(Generator generator)
            {
                var anonymous = new { int32 = random.Next(), float64 = random.NextDouble() };
                var buffer = generator.ToBytes(anonymous);
                var result = generator.ToValue(buffer, anonymous);

                Assert.IsFalse(ReferenceEquals(anonymous, result));
                Assert.AreEqual(anonymous, result);

            }

            validate(new Generator());
            validate(new Generator(null));
            validate(new Generator(Enumerable.Empty<Converter>()));
            validate(new Generator(new Converter[] { null }));
        }

        [TestMethod]
        public void AsToken()
        {
            var generator = new Generator();
            var source = new { id = 256, name = "sharp" };
            var buffer = generator.ToBytes(source);

            var token = generator.AsToken(buffer);
            Assert.IsTrue(token != null);
            Assert.IsTrue(token["id"].As<int>() == source.id);
            Assert.IsTrue(token["name"].As<string>() == source.name);
        }

        [TestMethod]
        public void GetConverter()
        {
            var generator = new Generator();
            var source = (1, "one");

            var ca = generator.GetConverter(source.GetType());
            var cb = generator.GetConverter<(int, string)>();
            Assert.IsTrue(ReferenceEquals(ca, cb));
        }

        [TestMethod]
        public void GetConverterArgumentNull()
        {
            var generator = new Generator();
            AssertExtension.MustFail<ArgumentNullException>(() => generator.GetConverter(null));
        }

        [TestMethod]
        public void GetConverterAnonymous()
        {
            var generator = new Generator();
            var source = new { id = 256, text = "data" };
            var ca = generator.GetConverter(source.GetType());
            var cb = generator.GetConverter(source);
            Assert.IsTrue(ReferenceEquals(ca, cb));
        }

        [TestMethod]
        public void ToBytesToValue()
        {
            var generator = new Generator();
            var source = Tuple.Create(Guid.NewGuid(), 1.0);

            var buffer = generator.ToBytes(source);
            var result = generator.ToValue<Tuple<Guid, double>>(buffer);
            Assert.IsTrue(Equals(source, result));
            Assert.IsFalse(ReferenceEquals(source, result));
        }

        [TestMethod]
        public void ToBytesToValueObject()
        {
            var generator = new Generator();
            var origin = new { id = "key", data = (Guid.NewGuid(), 1.0) };
            var source = (object)origin;

            var buffer = generator.ToBytes(source);
            var result = generator.ToValue(buffer, source.GetType());
            Assert.IsTrue(Equals(source, result));
            Assert.IsFalse(ReferenceEquals(source, result));
        }

        [TestMethod]
        public void ToBytesToValueAnonymous()
        {
            var generator = new Generator();
            var source = new { id = "item", data = (Guid.NewGuid(), 2.0) };

            var buffer = generator.ToBytes(source);
            var result = generator.ToValue(buffer, source);
            Assert.IsTrue(Equals(source, result));
            Assert.IsFalse(ReferenceEquals(source, result));
        }
    }
}
