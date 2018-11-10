using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Binary;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

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

        [TestMethod]
        public void Dynamic()
        {
            var source = new { guid = Guid.NewGuid(), data = new { name = "sharp", seed = 16 } };
            var buffer = cache.ToBytes(source);

            var token = cache.AsToken(buffer);
            var d = (dynamic)token;
            Assert.IsTrue((Guid)d.guid == source.guid);
            Assert.IsTrue((string)d.data.name == source.data.name);
            Assert.IsTrue((int)d.data.seed == source.data.seed);
        }

        [TestMethod]
        public void IReadOnlyDictionary()
        {
            var source = new { id = 24, name = "clock" };
            var buffer = cache.ToBytes(source);

            var token = cache.AsToken(buffer);
            var dictionary = (IReadOnlyDictionary<string, Token>)token;
            Assert.IsTrue(ReferenceEquals(token, dictionary));
            Assert.IsTrue(dictionary.ContainsKey("id"));
            Assert.IsFalse(dictionary.ContainsKey("none"));
            Assert.IsTrue(dictionary.Count == 2);
            Assert.IsTrue(dictionary.TryGetValue("name", out var name) && name.As<string>() == source.name);
            Assert.IsTrue(dictionary["name"].As<string>() == source.name);
            AssertExtension.MustFail<KeyNotFoundException>(() => dictionary["default"].As<string>());

            Assert.IsTrue(dictionary.Keys.Count() == 2);
            Assert.IsTrue(dictionary.Values.Count() == 2);

            var enumerator = ((IEnumerable)dictionary).GetEnumerator();
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsTrue(enumerator.MoveNext());
            Assert.IsFalse(enumerator.MoveNext());

            var genericEnumerator = dictionary.GetEnumerator();
            Assert.IsTrue(genericEnumerator.MoveNext());
            Assert.IsTrue(genericEnumerator.MoveNext());
            Assert.IsTrue(genericEnumerator.Current.Key == "name");
            Assert.IsFalse(genericEnumerator.MoveNext());
        }
    }
}
