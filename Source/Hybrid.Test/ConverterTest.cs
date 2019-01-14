using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Binary;
using Mikodev.Binary.Abstractions;
using System;
using System.Collections.Generic;

namespace Mikodev.Testing
{
    [TestClass]
    public class ConverterTest
    {
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

        private sealed class PersonConverter : VariableConverter<Person>
        {
            private readonly Converter<(int, string)> converter;

            public PersonConverter(Converter<(int, string)> converter)
            {
                this.converter = converter ?? throw new ArgumentNullException(nameof(converter));
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

        private sealed class PersonConverterCreator : IConverterCreator
        {
            public Converter GetConverter(IGeneratorContext context, Type type)
            {
                if (type != typeof(Person))
                    return null;
                var converter = context.GetConverter(typeof((int, string)));
                return new PersonConverter((Converter<(int, string)>)converter);
            }
        }

        [TestMethod]
        public void CustomConverter()
        {
            var creator = new PersonConverterCreator();
            var generator = new Generator(creators: new[] { creator });
            var person = (object)new Person { Id = 1024, Name = "sharp" };
            var buffer = generator.ToBytes(person);
            var result = generator.ToValue(buffer, typeof(Person));
            Assert.AreEqual(person, result);
        }

        [TestMethod]
        public void ObjectConverter()
        {
            var generator = new Generator();
            var converter = generator.GetConverter<object>();
            var source = (object)(1, 2.3);
            var allocator = new Allocator();
            converter.ToBytes(ref allocator, source);
            var buffer = allocator.ToArray();
            var result = generator.ToValue<(int, double)>(buffer);
            Assert.AreEqual(source, result);
        }
    }
}
