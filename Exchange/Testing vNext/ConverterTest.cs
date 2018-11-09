using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Binary;
using System;
using System.Collections.Generic;

namespace Mikodev.Testing
{
    [TestClass]
    public class ConverterTest
    {
        private sealed class Empty { }

        private sealed class EmptyConverter : Converter<Empty>
        {
            public bool Pass { get; private set; } = false;

            public EmptyConverter() : base(0) { }

            public override void ToBytes(ref Allocator allocator, Empty value)
            {
                Assert.IsTrue(GetConverter<int>() != null);
                Assert.IsTrue(GetConverter(typeof(double)) != null);
                Assert.IsTrue(GetConverter(new { id = 0 }) != null);
                AssertExtension.MustFail<ArgumentNullException>(() => GetConverter(null));
                Pass = true;
            }

            public override Empty ToValue(ReadOnlySpan<byte> memory)
            {
                throw new NotImplementedException();
            }

            public override void ToBytesAny(ref Allocator allocator, object value) => base.ToBytesAny(ref allocator, value);

            public override object ToValueAny(ReadOnlySpan<byte> memory) => base.ToValueAny(memory);
        }

        private sealed class Person : IEquatable<Person>
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public override bool Equals(object obj) => Equals(obj as Person);

            public bool Equals(Person other) =>
                other != null &&
                Id == other.Id &&
                Name == other.Name;

            public override int GetHashCode()
            {
                var hashCode = -1919740922;
                hashCode = hashCode * -1521134295 + Id.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Name);
                return hashCode;
            }
        }

        private sealed class PersonConverter : Converter<Person>
        {
            public PersonConverter() : base(0) { }

            public override void ToBytes(ref Allocator allocator, Person value) => throw new NotImplementedException();

            public override Person ToValue(ReadOnlySpan<byte> memory) => throw new NotImplementedException();

            public override void ToBytesAny(ref Allocator allocator, object value)
            {
                if (value == null)
                    return;
                var person = (Person)value;
                var converter = GetConverter<(int id, string name)>();
                converter.ToBytes(ref allocator, (person.Id, person.Name));
            }

            public override object ToValueAny(ReadOnlySpan<byte> memory)
            {
                if (memory.IsEmpty)
                    return null;
                var converter = GetConverter<(int id, string name)>();
                var (id, name) = converter.ToValue(memory);
                return new Person { Id = id, Name = name };
            }
        }

        [TestMethod]
        public void NotInitialized()
        {
            var converter = new EmptyConverter();
            AssertExtension.MustFail<InvalidOperationException>(() =>
            {
                var allocator = default(Allocator);
                converter.ToBytes(ref allocator, null);
            },
            x => x.Message.Contains("not initialized"));
        }

        [TestMethod]
        public void AlreadyInitialized()
        {
            var converter = new EmptyConverter();
            var _ = new Cache(new[] { converter });
            AssertExtension.MustFail<InvalidOperationException>(() => new Cache(new[] { converter }), x => x.Message.Contains("already initialized"));
        }

        [TestMethod]
        public void GetConverter()
        {
            var allocator = default(Allocator);
            var converter = new EmptyConverter();
            Assert.IsFalse(converter.Pass);
            converter.ToBytes(ref allocator, null);
            Assert.IsTrue(converter.Pass);
        }

        [TestMethod]
        public void CustomConverter()
        {
            var converter = new PersonConverter();
            var cache = new Cache(new[] { converter });
            var person = (object)new Person { Id = 1024, Name = "sharp" };
            var buffer = cache.ToBytes(person);
            var result = cache.ToValue(buffer, typeof(Person));
            Assert.AreEqual(person, result);
        }
    }
}
