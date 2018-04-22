using System;
using System.Collections.Generic;
using System.IO;

namespace Mikodev.Network
{
    public sealed partial class PacketWriter
    {
        internal sealed class Item
        {
            internal const int Bytes = 1;
            internal const int MemoryStream = 2;
            internal const int ArrayBytes = 3;
            internal const int ListItem = 4;
            internal const int DictionaryPacketWriter = 5;
            internal const int DictionaryBytesBytes = 6;
            internal const int DictionaryBytesItem = 7;

            internal static readonly Item Empty = new Item();
            internal static readonly byte[] s_zero_bytes = new byte[sizeof(int)];

            internal readonly object obj;
            internal readonly int tag;
            internal readonly int lenone;
            internal readonly int lentwo;

            private Item() { }

            internal Item(byte[] buffer)
            {
                obj = buffer;
                tag = Bytes;
            }

            internal Item(MemoryStream stream)
            {
                obj = stream;
                tag = MemoryStream;
            }

            internal Item(List<Item> list)
            {
                obj = list;
                tag = ListItem;
            }

            internal Item(Dictionary<string, PacketWriter> dictionary)
            {
                obj = dictionary;
                tag = DictionaryPacketWriter;
            }

            internal Item(byte[][] array, int length)
            {
                obj = array;
                tag = ArrayBytes;
                lenone = length;
            }

            internal Item(List<KeyValuePair<byte[], Item>> dictionary, int length)
            {
                obj = dictionary;
                tag = DictionaryBytesItem;
                lenone = length;
            }

            internal Item(List<KeyValuePair<byte[], byte[]>> dictionary, int indexLength, int elementLength)
            {
                obj = dictionary;
                tag = DictionaryBytesBytes;
                lenone = indexLength;
                lentwo = elementLength;
            }

            internal void GetBytesMatch(Stream stream, int level)
            {
                if (level > Cache.Limits)
                    throw new PacketException(PacketError.RecursiveError);
                level += 1;

                switch (tag)
                {
                    case ArrayBytes:
                        {
                            var byt = (byte[][])obj;
                            if (lenone > 0)
                                for (int i = 0; i < byt.Length; i++)
                                    stream.Write(byt[i]);
                            else
                                for (int i = 0; i < byt.Length; i++)
                                    stream.WriteExt(byt[i]);
                            break;
                        }
                    case ListItem:
                        {
                            var lst = (List<Item>)obj;
                            for (int i = 0; i < lst.Count; i++)
                                lst[i].GetBytes(stream, level);
                            break;
                        }
                    case DictionaryPacketWriter:
                        {
                            var dic = (Dictionary<string, PacketWriter>)obj;
                            foreach (var i in dic)
                            {
                                stream.WriteKey(i.Key);
                                i.Value.item.GetBytes(stream, level);
                            }
                            break;
                        }
                    case DictionaryBytesBytes:
                        {
                            var itr = (List<KeyValuePair<byte[], byte[]>>)obj;
                            for (int i = 0; i < itr.Count; i++)
                            {
                                var cur = itr[i];
                                if (lenone > 0)
                                    stream.Write(cur.Key, 0, lenone);
                                else
                                    stream.WriteExt(cur.Key);
                                if (lentwo > 0)
                                    stream.Write(cur.Value, 0, lentwo);
                                else
                                    stream.WriteExt(cur.Value);
                            }
                            break;
                        }
                    case DictionaryBytesItem:
                        {
                            var kvp = (List<KeyValuePair<byte[], Item>>)obj;
                            for (int i = 0; i < kvp.Count; i++)
                            {
                                var cur = kvp[i];
                                if (lenone > 0)
                                    stream.Write(cur.Key, 0, lenone);
                                else
                                    stream.WriteExt(cur.Key);
                                cur.Value.GetBytes(stream, level);
                            }
                            break;
                        }
                    default: throw new ApplicationException();
                }
            }

            internal void GetBytes(Stream stream, int level)
            {
                if (level > Cache.Limits)
                    throw new PacketException(PacketError.RecursiveError);
                level += 1;

                if (obj == null)
                {
                    stream.Write(s_zero_bytes, 0, sizeof(int));
                }
                else if (tag == Bytes)
                {
                    stream.WriteExt((byte[])obj);
                }
                else if (tag == MemoryStream)
                {
                    stream.WriteExt((MemoryStream)obj);
                }
                else
                {
                    stream.BeginInternal(out var src);
                    GetBytesMatch(stream, level);
                    stream.FinshInternal(src);
                }
            }
        }
    }
}
