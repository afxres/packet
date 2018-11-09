using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Binary;
using System;
using System.Collections.Generic;

namespace Mikodev.Testing
{
    [TestClass]
    public class TokenTest
    {
        private readonly Cache cache = new Cache();

        [TestMethod]
        public void As()
        {
            var source = new { id = 1, data = new { name = "sharp" } };
            var buffer = cache.ToBytes(source);

            var token = cache.AsToken(buffer);
            Assert.IsTrue(token["id"].As<int>() == source.id);
            var data = token["data"];
            Assert.IsTrue(data != null);
            Assert.IsTrue(data["name"].As<string>() == source.data.name);
        }

        [TestMethod]
        public void AsObject()
        {
            var source = new { single = 1.23F, text = "float" };
            var buffer = cache.ToBytes(source);

            var token = cache.AsToken(buffer);
            Assert.AreEqual(token["single"].As(typeof(float)), source.single);
            Assert.AreEqual(token["text"].As(typeof(string)), source.text);
        }

        [TestMethod]
        public void AsAnonymous()
        {
            var source = new { name = "sharp", data = Math.E };
            var buffer = cache.ToBytes(source);

            var token = cache.AsToken(buffer);
            var result = token.As(source);
            Assert.IsFalse(ReferenceEquals(source, result));
            Assert.AreEqual(source, result);
        }

        [TestMethod]
        public void At()
        {
            var source = new { alpha = "ann" };
            var buffer = cache.ToBytes(source);

            var token = cache.AsToken(buffer);
            Assert.IsTrue(token.At("alpha").As<string>() == source.alpha);
            Assert.IsTrue(token.At("bravo") == null);
        }

        [TestMethod]
        public void Indexer()
        {
            var source = new { alpha = "alice" };
            var buffer = cache.ToBytes(source);

            var token = cache.AsToken(buffer);
            Assert.IsTrue(token["alpha"].As<string>() == source.alpha);
            AssertExtension.MustFail<KeyNotFoundException>(() => token["bravo"].ToString());
        }
    }
}
