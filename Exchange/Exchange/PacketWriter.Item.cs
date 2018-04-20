using System;
using System.Collections.Generic;
using System.IO;

namespace Mikodev.Network
{
    public sealed partial class PacketWriter
    {
        internal sealed class Item
        {
            internal const int Null = 0;
            internal const int Bytes = 1;
            internal const int MemoryStream = 2;
            internal const int ArrayBytes = 3;
            internal const int ListItem = 4;
            internal const int DictionaryPacketWriter = 5;
            internal const int DictionaryBytesBytes = 6;
            internal const int DictionaryBytesItem = 7;

            internal static readonly Item Empty = new Item();

            internal readonly object obj;
            internal readonly int tag;
            internal readonly int lenone;
            internal readonly int lentwo;

            private Item() { }

            internal Item(byte[] buf)
            {
                obj = buf;
                tag = Bytes;
            }

            internal Item(MemoryStream mst)
            {
                obj = mst;
                tag = MemoryStream;
            }

            internal Item(List<Item> lst)
            {
                obj = lst;
                tag = ListItem;
            }

            internal Item(Dictionary<string, PacketWriter> dic)
            {
                obj = dic;
                tag = DictionaryPacketWriter;
            }

            internal Item(byte[][] arr, int len)
            {
                obj = arr;
                tag = ArrayBytes;
                lenone = len;
            }

            internal Item(List<KeyValuePair<byte[], Item>> lst, int one)
            {
                obj = lst;
                tag = DictionaryBytesItem;
                lenone = one;
            }

            internal Item(List<KeyValuePair<byte[], byte[]>> lst, int one, int two)
            {
                obj = lst;
                tag = DictionaryBytesBytes;
                lenone = one;
                lentwo = two;
            }

            internal void GetBytesMatch(Stream str, int lev)
            {
                if (lev > Cache.Depth)
                    throw new PacketException(PacketError.RecursiveError);
                lev += 1;

                switch (tag)
                {
                    case ArrayBytes:
                        {
                            var byt = (byte[][])obj;
                            if (lenone > 0)
                                for (int i = 0; i < byt.Length; i++)
                                    str.Write(byt[i]);
                            else
                                for (int i = 0; i < byt.Length; i++)
                                    str.WriteExt(byt[i]);
                            break;
                        }
                    case ListItem:
                        {
                            var lst = (List<Item>)obj;
                            for (int i = 0; i < lst.Count; i++)
                                lst[i].GetBytes(str, lev);
                            break;
                        }
                    case DictionaryPacketWriter:
                        {
                            var dic = (Dictionary<string, PacketWriter>)obj;
                            foreach (var i in dic)
                            {
                                str.WriteKey(i.Key);
                                i.Value.item.GetBytes(str, lev);
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
                                    str.Write(cur.Key, 0, lenone);
                                else
                                    str.WriteExt(cur.Key);
                                if (lentwo > 0)
                                    str.Write(cur.Value, 0, lentwo);
                                else
                                    str.WriteExt(cur.Value);
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
                                    str.Write(cur.Key, 0, lenone);
                                else
                                    str.WriteExt(cur.Key);
                                cur.Value.GetBytes(str, lev);
                            }
                            break;
                        }
                    default: throw new ApplicationException();
                }
            }

            internal void GetBytes(Stream str, int lev)
            {
                if (lev > Cache.Depth)
                    throw new PacketException(PacketError.RecursiveError);
                lev += 1;

                if (obj == null)
                {
                    str.Write(Extension.s_zero_bytes, 0, sizeof(int));
                }
                else if (tag == Bytes)
                {
                    str.WriteExt((byte[])obj);
                }
                else if (tag == MemoryStream)
                {
                    str.WriteExt((MemoryStream)obj);
                }
                else
                {
                    str.BeginInternal(out var src);
                    GetBytesMatch(str, lev);
                    str.FinshInternal(src);
                }
            }
        }
    }
}
