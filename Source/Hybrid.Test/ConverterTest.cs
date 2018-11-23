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
            public bool Initialized { get; private set; } = false;

            public Converter<(int, string)> Converter { get; private set; }

            public EmptyConverter() : base(0) { }

            protected override void OnInitialize(Cache cache)
            {
                base.OnInitialize(cache);
                Initialized = true;
                Converter = cache.GetConverter<(int, string)>();
            }

            public override void ToBytes(ref Allocator allocator, Empty value) => throw new NotImplementedException();

            public override Empty ToValue(ReadOnlySpan<byte> memory) => throw new NotImplementedException();

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
            private Converter<(int, string)> converter;

            public PersonConverter() : base(0) { }

            protected override void OnInitialize(Cache cache)
            {
                converter = cache.GetConverter<(int, string)>();
            }

            public override void ToBytes(ref Allocator allocator, Person value) => throw new NotImplementedException();

            public override Person ToValue(ReadOnlySpan<byte> memory) => throw new NotImplementedException();

            public override void ToBytesAny(ref Allocator allocator, object value)
            {
                if (value == null)
                    return;
                var person = (Person)value;
                converter.ToBytes(ref allocator, (person.Id, person.Name));
            }

            public override object ToValueAny(ReadOnlySpan<byte> memory)
            {
                if (memory.IsEmpty)
                    return null;
                var (id, name) = converter.ToValue(memory);
                return new Person { Id = id, Name = name };
            }
        }

        [TestMethod]
        public void Initialize()
        {
            var converter = new EmptyConverter();
            Assert.IsFalse(converter.Initialized);
            Assert.IsTrue(converter.Converter == null);
            var _ = new Cache(new[] { converter });
            Assert.IsTrue(converter.Initialized);
            Assert.IsTrue(converter.Converter != null);
        }

        [TestMethod]
        public void AlreadyInitialized()
        {
            var converter = new EmptyConverter();
            var _ = new Cache(new[] { converter });
            AssertExtension.MustFail<InvalidOperationException>(() => new Cache(new[] { converter }), x => x.Message.Contains("already initialized"));
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

        [TestMethod]
        public void ObjectConverter()
        {
            var cache = new Cache();
            var converter = cache.GetConverter<object>();
            var source = (object)(1, 2.3);
            var allocator = new Allocator();
            converter.ToBytes(ref allocator, source);
            var buffer = allocator.ToArray();
            var result = cache.ToValue<(int, double)>(buffer);
            Assert.AreEqual(source, result);
        }

        [TestMethod]
        public void ObjectConverterNotInitialized()
        {
            // HACK!
            var type = new Cache().GetConverter<object>().GetType();
            var converter = (Converter<object>)Activator.CreateInstance(type);

            AssertExtension.MustFail<InvalidOperationException>(() =>
            {
                var allocator = new Allocator();
                converter.ToBytes(ref allocator, string.Empty);
            },
            x => x.Message.Contains("not initialized"));
        }
    }
}
