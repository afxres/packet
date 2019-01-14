using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Binary;
using System;
using System.Linq;

namespace Mikodev.Testing
{
    [TestClass]
    public class CircularReferenceTest
    {
        private sealed class LinkedNode<T>
        {
            public LinkedNode<T> Next { get; set; }

            public T Item { get; set; }
        }

        private readonly Generator generator = new Generator();

        [TestMethod]
        public void LinkedList()
        {
            try
            {
                var linked = Enumerable.Range(0, 9).Aggregate(default(LinkedNode<int>), (last, index) => new LinkedNode<int> { Next = last, Item = index });
                var buffer = generator.ToBytes(linked);
                Assert.Fail();
            }
            catch (ArgumentException ex)
            {
                Assert.IsTrue(ex.Message.StartsWith("Circular type reference"));
            }
        }
    }
}
