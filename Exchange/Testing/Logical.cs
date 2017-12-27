using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Network;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using static Mikodev.Testing.Extensions;

namespace Mikodev.Testing
{
    internal class _Ref { }

    internal class _Converter : IPacketConverter
    {
        internal static readonly string _BytesErr = Guid.NewGuid().ToString();

        internal static readonly string _ValueErr = Guid.NewGuid().ToString();

        int IPacketConverter.Length => 0;

        byte[] IPacketConverter.GetBytes(object value) => throw new Exception(_BytesErr);

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length) => throw new OutOfMemoryException(_ValueErr);
    }

    internal class _Empty { }

    internal class _Person
    {
        public int Age { get; set; }

        public string Name { get; set; }
    }

    internal class _ReadOnly
    {
        public int Number { get; }

        public string Text { get; }

        public _ReadOnly(int num, string text)
        {
            Number = num;
            Text = text;
        }
    }

    internal class _WriteOnly
    {
        public string Name => nameof(_WriteOnly);

        public string Value { set { } }
    }
    
    internal struct _Value
    {
        public int One { get; set; }

        public string Two { get; set; }
    }

    [TestClass]
    public class Logical
    {
        [TestMethod]
        public void Rethrow()
        {
            var val = new _Ref();
            var con = new Dictionary<Type, IPacketConverter> { [typeof(_Ref)] = new _Converter() };

            try
            {
                var buf = new PacketRawWriter(con).Push(val);
                Assert.Fail();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.ConvertError && ex.InnerException.Message == _Converter._BytesErr)
            {
                // ignore
            }

            try
            {
                var res = new PacketRawReader(new byte[4], con).Pull<_Ref>();
            }
            catch (Exception ex) when (ex.Message == _Converter._ValueErr)
            {
                // ignore
            }
        }

        [TestMethod]
        public void SpecialSerialize()
        {
            var msg = "Hello, exception!";
            var e = new Exception(msg);
            var pkt = PacketWriter.Serialize(e);
            var buf = pkt.GetBytes();

            var rea = new PacketReader(buf);
            Assert.AreEqual(msg, rea[nameof(Exception.Message)].GetValue<string>());
        }

        [TestMethod]
        public void MultiThread()
        {
            var lst = new List<Task>();
            const int _max = 1 << 16;
            const int _tasks = 16;
            for (int i = 0; i < _tasks; i++)
            {
                var idx = i;
                lst.Add(new Task(() =>
                {
                    for (int k = 0; k < _max; k++)
                    {
                        var obj = new
                        {
                            task = idx,
                            data = k,
                        };
                        var pkt = PacketWriter.Serialize(obj);
                        var buf = pkt.GetBytes();
                        var rea = new PacketReader(buf);
                        Assert.AreEqual(idx, rea["task"].GetValue<int>());
                        Assert.AreEqual(k, rea["data"].GetValue<int>());
                    }
                }));
            }

            lst.ForEach(r => r.Start());
            lst.ForEach(r => r.Wait());
        }

        [TestMethod]
        public void DeserializeAnonymous()
        {
            var a = new
            {
                id = 0,
                name = "Bob",
                data = new
                {
                    array = new[] { 1, 2, 3, 4 },
                    buffer = new byte[] { 1, 2, 3, 4 },
                }
            };
            var pkt = PacketWriter.Serialize(a);
            var buf = pkt.GetBytes();
            var rea = new PacketReader(buf);
            var val = rea.Deserialize(new { id = 0, name = string.Empty, data = new { array = default(int[]), buffer = default(byte[]) } });

            Assert.AreEqual(a.id, val.id);
            Assert.AreEqual(a.name, val.name);

            ThrowIfNotSequenceEqual(a.data.array, val.data.array);
            ThrowIfNotSequenceEqual(a.data.buffer, val.data.buffer);
        }

        [TestMethod]
        public void Deserialize()
        {
            var p = new _Person() { Age = 20, Name = "Bob" };
            var pkt = PacketWriter.Serialize(p);
            var buf = pkt.GetBytes();
            var rea = new PacketReader(buf);
            var val = rea.Deserialize<_Person>();

            Assert.AreEqual(p.Age, val.Age);
            Assert.AreEqual(p.Name, val.Name);
        }

        [TestMethod]
        public void DeserializeReadOnly()
        {
            var a = new _ReadOnly(10, "read");
            var pkt = PacketWriter.Serialize(a);
            var buf = pkt.GetBytes();
            var rea = new PacketReader(buf);

            try
            {
                var val = rea.Deserialize<_ReadOnly>();
                Assert.Fail();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.InvalidType)
            {
                // ignore
            }
        }

        [TestMethod]
        public void DeserializeWriteOnly()
        {
            var a = new _WriteOnly();
            var pkt = PacketWriter.Serialize(a);
            var buf = pkt.GetBytes();
            var rea = new PacketReader(buf);
            var obj = rea.Deserialize<_WriteOnly>();
            var val = rea.GetItem(nameof(_WriteOnly.Value), true);

            Assert.AreEqual(val, null);
        }

        [TestMethod]
        public void DeserializeEmpty()
        {
            var a = new _Empty();
            var pkt = PacketWriter.Serialize(a);
            var buf = pkt.GetBytes();
            var rea = new PacketReader(buf);
            var obj = rea.Deserialize<_Empty>();

            Assert.AreEqual(buf.Length, 0);
        }

        [TestMethod]
        public void DeserializeStructural()
        {
            var v = new _Value { One = 1, Two = "Two" };
            var pkt = PacketWriter.Serialize(v);
            var buf = pkt.GetBytes();
            var rea = new PacketReader(buf);
            var obj = rea.Deserialize<_Value>();

            Assert.AreEqual(obj.One, 1);
            Assert.AreEqual(obj.Two, "Two");
        }
    }
}
