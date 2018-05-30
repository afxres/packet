using System;
using System.Collections.Generic;
using System.IO;

namespace Mikodev.Network
{
    partial class PacketWriter
    {
        internal sealed class Item
        {
            internal static readonly Item Empty = new Item();
            internal static readonly byte[] ZeroBytes = new byte[sizeof(int)];

            internal readonly object value;
            internal readonly ItemFlags flag;
            internal readonly int lengthOne;
            internal readonly int lengthTwo;

            private Item() { }

            internal Item(byte[] source)
            {
                if (source == null)
                    return;
                value = source;
                flag = ItemFlags.Buffer;
            }

            internal Item(MemoryStream source)
            {
                if (source == null)
                    return;
                value = source;
                flag = ItemFlags.Stream;
            }

            internal Item(List<Item> source)
            {
                if (source == null)
                    return;
                value = source;
                flag = ItemFlags.ItemList;
            }

            internal Item(Dictionary<string, PacketWriter> source)
            {
                if (source == null)
                    return;
                value = source;
                flag = ItemFlags.Dictionary;
            }

            internal Item(byte[][] source, int length)
            {
                if (source == null)
                    return;
                value = source;
                flag = ItemFlags.BufferArray;
                lengthOne = length;
            }

            internal Item(List<KeyValuePair<byte[], Item>> source, int length)
            {
                if (source == null)
                    return;
                value = source;
                flag = ItemFlags.DictionaryBufferItem;
                lengthOne = length;
            }

            internal Item(List<KeyValuePair<byte[], byte[]>> source, int indexLength, int elementLength)
            {
                if (source == null)
                    return;
                value = source;
                flag = ItemFlags.DictionaryBuffer;
                lengthOne = indexLength;
                lengthTwo = elementLength;
            }

            internal void GetBytes(Stream stream, int level)
            {
                PacketException.VerifyRecursionError(ref level);
                switch (flag)
                {
                    case ItemFlags.None:
                        stream.Write(ZeroBytes);
                        break;
                    case ItemFlags.Buffer:
                        stream.WriteExt((byte[])value);
                        break;
                    case ItemFlags.Stream:
                        stream.WriteExt((MemoryStream)value);
                        break;
                    default:
                        stream.BeginInternal(out var src);
                        GetBytesMatch(stream, level);
                        stream.FinshInternal(src);
                        break;
                }
            }

            internal void GetBytesMatch(Stream stream, int level)
            {
                PacketException.VerifyRecursionError(ref level);
                switch (flag)
                {
                    case ItemFlags.BufferArray:
                        GetBytesMatchBufferArray(stream);
                        break;
                    case ItemFlags.ItemList:
                        GetBytesMatchItemList(stream, level);
                        break;
                    case ItemFlags.Dictionary:
                        GetBytesMatchDictionary(stream, level);
                        break;
                    case ItemFlags.DictionaryBuffer:
                        GetBytesMatchDictionaryBuffer(stream);
                        break;
                    case ItemFlags.DictionaryBufferItem:
                        GetBytesMatchDictionaryBufferItem(stream, level);
                        break;
                    default: throw new ApplicationException();
                }
            }

            private void GetBytesMatchBufferArray(Stream stream)
            {
                var array = (byte[][])value;
                if (lengthOne > 0)
                    for (int i = 0; i < array.Length; i++)
                        stream.Write(array[i]);
                else
                    for (int i = 0; i < array.Length; i++)
                        stream.WriteExt(array[i]);
            }

            private void GetBytesMatchItemList(Stream stream, int level)
            {
                var list = (List<Item>)value;
                for (int i = 0; i < list.Count; i++)
                    list[i].GetBytes(stream, level);
            }

            private void GetBytesMatchDictionary(Stream stream, int level)
            {
                var dictionary = (Dictionary<string, PacketWriter>)value;
                foreach (var i in dictionary)
                {
                    stream.WriteKey(i.Key);
                    i.Value.item.GetBytes(stream, level);
                }
            }

            private void GetBytesMatchDictionaryBuffer(Stream stream)
            {
                var list = (List<KeyValuePair<byte[], byte[]>>)value;
                for (int i = 0; i < list.Count; i++)
                {
                    var current = list[i];
                    if (lengthOne > 0)
                        stream.Write(current.Key, 0, lengthOne);
                    else
                        stream.WriteExt(current.Key);
                    if (lengthTwo > 0)
                        stream.Write(current.Value, 0, lengthTwo);
                    else
                        stream.WriteExt(current.Value);
                }
            }

            private void GetBytesMatchDictionaryBufferItem(Stream stream, int level)
            {
                var list = (List<KeyValuePair<byte[], Item>>)value;
                for (int i = 0; i < list.Count; i++)
                {
                    var current = list[i];
                    if (lengthOne > 0)
                        stream.Write(current.Key, 0, lengthOne);
                    else
                        stream.WriteExt(current.Key);
                    current.Value.GetBytes(stream, level);
                }
            }
        }
    }
}
