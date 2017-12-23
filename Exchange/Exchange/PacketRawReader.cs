using System;
using ConverterDictionary = System.Collections.Generic.IDictionary<System.Type, Mikodev.Network.IPacketConverter>;

namespace Mikodev.Network
{
    /// <summary>
    /// 原始数据包解析工具. Raw reader without any format
    /// </summary>
    public sealed class PacketRawReader
    {
        internal _Element _spa;
        internal readonly ConverterDictionary _con;

        /// <summary>
        /// 创建对象. Create reader
        /// </summary>
        public PacketRawReader(PacketReader source)
        {
            _spa = new _Element(source._spa);
            _con = source._con;
        }

        /// <summary>
        /// 创建对象并指定字节数组和转换器. Create reader with byte array and converters
        /// </summary>
        public PacketRawReader(byte[] buffer, ConverterDictionary converters = null)
        {
            _spa = new _Element(buffer);
            _con = converters;
        }

        /// <summary>
        /// 创建对象并指定部分字节数组和转换器. Create reader with part of byte array and converters
        /// </summary>
        public PacketRawReader(byte[] buffer, int offset, int length, ConverterDictionary converters = null)
        {
            _spa = new _Element(buffer, offset, length);
            _con = converters;
        }

        /// <summary>
        /// 是否可以继续读取. Current index is not at the end of the buffer
        /// </summary>
        public bool Any => _spa._Any();

        /// <summary>
        /// 读取一个目标类型对象. Get value with target type
        /// </summary>
        public object Pull(Type type)
        {
            var con = _Caches.Converter(type, _con, false);
            var res = _spa._Next(con);
            return res;
        }

        /// <summary>
        /// 读取一个目标类型对象 (泛型). Get value with target type (Generic)
        /// </summary>
        public T Pull<T>()
        {
            var con = _Caches.Converter(typeof(T), _con, false);
            var res = _spa._Next<T>(con);
            return res;
        }

        /// <summary>
        /// 重置读取索引. Move current position to origin
        /// </summary>
        public void Reset()
        {
            _spa._idx = _spa._off;
        }

        /// <summary>
        /// 打印对象类型和字节长度. Show type and byte count
        /// </summary>
        public override string ToString() => $"{nameof(PacketRawReader)} with {_spa._len} byte(s)";
    }
}
