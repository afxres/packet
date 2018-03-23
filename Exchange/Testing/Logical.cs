using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mikodev.Network;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    internal class _BadConverter : IPacketConverter
    {
        int IPacketConverter.Length => 4;

        byte[] IPacketConverter.GetBytes(object value)
        {
            return null;
        }

        object IPacketConverter.GetValue(byte[] buffer, int offset, int length)
        {
            return new byte[1];
        }
    }

    internal class _Two
    {
        public int One { get; set; }

        public int Two { get; set; }
    }

    internal class _TwoConverter : IPacketConverter
    {
        public int Length => sizeof(int) * 2;

        public byte[] GetBytes(object value)
        {
            var two = (_Two)value;
            var buf = new byte[sizeof(int) * 2];
            var a = BitConverter.GetBytes(two.One);
            var b = BitConverter.GetBytes(two.Two);
            Buffer.BlockCopy(a, 0, buf, 0, sizeof(int));
            Buffer.BlockCopy(b, 0, buf, sizeof(int), sizeof(int));
            return buf;
        }

        public object GetValue(byte[] buffer, int offset, int length)
        {
            if (length < (sizeof(int) * 2))
                throw new ArgumentOutOfRangeException(nameof(length));
            var a = BitConverter.ToInt32(buffer, offset);
            var b = BitConverter.ToInt32(buffer, offset + sizeof(int));
            var two = new _Two { One = a, Two = b };
            return two;
        }
    }

    internal class _Box
    {
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            if (obj is _Box box)
                return Name.Equals(box.Name);
            return false;
        }

        public override int GetHashCode()
        {
            return Name?.GetHashCode() ?? 0;
        }
    }

    internal class _BoxConverter : IPacketConverter
    {
        public int Length => 0;

        public byte[] GetBytes(object value)
        {
            return Encoding.UTF8.GetBytes(((_Box)value).Name);
        }

        public object GetValue(byte[] buffer, int offset, int length)
        {
            return new _Box { Name = Encoding.UTF8.GetString(buffer, offset, length) };
        }
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

    internal class _Tuple : IEnumerable<KeyValuePair<int, string>>
    {
        public IEnumerator<KeyValuePair<int, string>> GetEnumerator()
        {
            for (int i = 0; i < 8; i++)
            {
                yield return new KeyValuePair<int, string>(i, i.ToString());
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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
                var buf = new PacketRawWriter(con).SetValue(val);
                Assert.Fail();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.ConvertError && ex.InnerException.Message == _Converter._BytesErr)
            {
                // ignore
            }

            try
            {
                var res = new PacketRawReader(new byte[4], con).GetValue<_Ref>();
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
            Task.WaitAll(lst.ToArray());
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
        public void DeserializeOffset()
        {
            var obj = new { id = 1.1, text = "what the" };
            var ta = PacketConvert.Serialize(obj);
            var buf = new byte[ta.Length + 128];
            var off = new Random().Next(8, 32);
            Buffer.BlockCopy(ta, 0, buf, off, ta.Length);

            var ra = PacketConvert.Deserialize(ta, obj.GetType());
            var rb = PacketConvert.Deserialize(buf, off, ta.Length, obj.GetType());

            void AreEqual(object a, object b)
            {
                var x = a.Cast(obj);
                var y = b.Cast(obj);
                if (x.id == y.id && x.text == y.text)
                    return;
                throw new ApplicationException();
            }

            AreEqual(obj, ra);
            AreEqual(obj, rb);

            var str = "world";
            var tb = PacketConvert.GetBytes(str);
            var tc = new byte[tb.Length + 128];
            Buffer.BlockCopy(tb, 0, tc, off, tb.Length);
            var rc = PacketConvert.Deserialize<string>(tc, off, tb.Length);
            Assert.AreEqual(str, rc);
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

        [TestMethod]
        public void ConvertMismatch()
        {
            var cvt = new Dictionary<Type, IPacketConverter>() { [typeof(_Ref)] = new _BadConverter() };

            try
            {
                var buf = PacketConvert.Serialize(new _Ref(), cvt);
                Assert.Fail();
            }
            catch (PacketException ex) when (ex.ErrorCode == PacketError.ConvertMismatch)
            {
                // ignore
            }
        }

        [TestMethod]
        public void ISet()
        {
            var set = new HashSet<int>() { 1, 2, 3, 4 };
            var buf = PacketConvert.Serialize(set);
            var res = PacketConvert.Deserialize<HashSet<int>>(buf);
            var val = set.SetEquals(res);
            Assert.AreEqual(set.Count, res.Count);
            Assert.AreEqual(val, true);
            return;
        }


        [TestMethod]
        public void LegacyConverter()
        {
            var con = new Dictionary<Type, IPacketConverter>()
            {
                [typeof(_Two)] = new _TwoConverter(),
                [typeof(_Box)] = new _BoxConverter(),
            };
            var obj = new
            {
                a = Enumerable.Range(0, 4).Select(r => new _Two { One = r, Two = r * 2 }),
                b = Enumerable.Range(0, 8).ToDictionary(r => r.ToString(), r => new _Two { One = r * 2, Two = r * 4 }),
            };
            var buf = PacketConvert.Serialize(obj, con);
            var rea = new PacketReader(buf, con);
            var itr = rea["a"].GetEnumerable<_Two>();

            var oc = new _Tuple();
            var c = oc.ToDictionary(r => r.Key, r => r.Value);
            var kvp = PacketWriter.Serialize(oc);
            var tc = kvp.GetBytes();

            var ra = itr.ToList();
            var rb = rea["b"].GetDictionary<string, _Two>();
            var rc = PacketConvert.Deserialize<IDictionary<int, string>>(tc);

            var od = new[] { new _Box { Name = "one" }, new _Box { Name = "Loooooooooooooong name!" } };
            var td = PacketConvert.Serialize(od, con);
            var rd = PacketConvert.Deserialize<IEnumerable<_Box>>(td, con);

            ThrowIfNotEqual(c, rc);
            ThrowIfNotSequenceEqual(od, rd);
            return;
        }
    }
}
