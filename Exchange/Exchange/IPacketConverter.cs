namespace Mikodev.Network
{
    /// <summary>
    /// 转换器基础接口. Basic interface for converter
    /// </summary>
    public interface IPacketConverter
    {
        /// <summary>
        /// 对象到字节数组. object -> byte array
        /// </summary>
        byte[] GetBytes(object value);

        /// <summary>
        /// 字节数组到对象. byte array -> object
        /// </summary>
        object GetValue(byte[] buffer, int offset, int length);

        /// <summary>
        /// 对象的字节长度, 若长度不固定则返回 0. byte length, return zero if length not constant
        /// </summary>
        int Length { get; }
    }

    /// <summary>
    /// 转换器泛型接口. Generic interface for converter
    /// </summary>
    public interface IPacketConverter<T> : IPacketConverter
    {
        /// <summary>
        /// 泛型对象到字节数组. T -> byte array
        /// </summary>
        byte[] GetBytes(T value);

        /// <summary>
        /// 字节数组到泛型对象. byte array -> T
        /// </summary>
        new T GetValue(byte[] buffer, int offset, int length);
    }
}
