using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Binary;
using System;
using System.Collections.Generic;

namespace Mikodev.Testing
{
    [TestClass]
    public class ConverterTest
    {
        private sealed class EmptyClass { }

        private sealed class EmptyClassConverter : Converter<EmptyClass>
        {
            public bool Tested { get; private set; } = false;

            public EmptyClassConverter() : base(0) { }

            public override void ToBytes(Allocator allocator, EmptyClass value)
            {
                Assert.IsTrue(GetConverter<int>() != null);
                Assert.IsTrue(GetConverter(typeof(double)) != null);
                AssertExtension.MustFail<ArgumentNullException>(() => GetConverter(null));
                Tested = true;
            }

            public override EmptyClass ToValue(ReadOnlyMemory<byte> memory)
            {
                throw new NotImplementedException();
            }

            public override void ToBytesAny(Allocator allocator, object value) => base.ToBytesAny(allocator, value);

            public override object ToValueAny(ReadOnlyMemory<byte> memory) => base.ToValueAny(memory);
        }

        private sealed class Person : IEquatable<Person>
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public override bool Equals(object obj) => Equals(obj as Person);

            public bool Equals(Person other) =>
                other != null &&
                this.Id == other.Id &&
                this.Name == other.Name;

            public override int GetHashCode()
            {
                var hashCode = -1919740922;
                hashCode = hashCode * -1521134295 + this.Id.GetHashCode();
                hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.Name);
                return hashCode;
            }
        }

        private sealed class PersonConverter : Converter<Person>
        {
            public PersonConverter() : base(0) { }

            public override void ToBytes(Allocator allocator, Person value) => throw new NotImplementedException();

            public override Person ToValue(ReadOnlyMemory<byte> memory) => throw new NotImplementedException();

            public override void ToBytesAny(Allocator allocator, object value)
            {
                if (value == null)
                    return;
                var person = (Person)value;
                var converter = GetConverter<(int id, string name)>();
                converter.ToBytes(allocator, (person.Id, person.Name));
            }

            public override object ToValueAny(ReadOnlyMemory<byte> memory)
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
            var converter = new EmptyClassConverter();
            AssertExtension.MustFail<InvalidOperationException>(() => converter.ToBytes(null, null), x => x.Message.Contains("not initialized"));
        }

        [TestMethod]
        public void AlreadyInitialized()
        {
            var converter = new EmptyClassConverter();
            var c1 = new Cache(new[] { converter });
            AssertExtension.MustFail<InvalidOperationException>(() => new Cache(new[] { converter }), x => x.Message.Contains("already initialized"));
            converter.ToBytes(null, null);
            Assert.IsTrue(converter.Tested);
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
