using System;
using System.Runtime.Serialization;

namespace Mikodev.Network
{
    /// <summary>
    /// 由数据溢出, 转换错误等情况引发的异常. Exception cause by overflow, converter not found, etc
    /// </summary>
    [Serializable]
    public sealed class PacketException : Exception
    {
        internal readonly PacketError _code = PacketError.None;

        internal static string _GetMessage(PacketError code)
        {
            switch (code)
            {
                case PacketError.ConvertError:
                    return "Convert failed, see inner exception for more information";
                case PacketError.Overflow:
                    return "Data length overflow";
                case PacketError.PathError:
                    return "Path not exists";
                case PacketError.RecursiveError:
                    return "Recursion limit has been reached";
                case PacketError.InvalidType:
                    return "Invalid type";
                default:
                    return "Undefined error";
            }
        }

        /// <summary>
        /// 错误代码. Error code
        /// </summary>
        public PacketError ErrorCode => _code;

        /// <summary>
        /// 创建对象并指定异常代码. Create new instance with error code
        /// </summary>
        public PacketException(PacketError code) : base(_GetMessage(code)) => _code = code;

        /// <summary>
        /// 创建对象并指定异常代码和内部异常. Create new instance with error code and inner exception
        /// </summary>
        public PacketException(PacketError code, Exception except) : base(_GetMessage(code), except) => _code = code;

        internal PacketException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            _code = (PacketError)info.GetValue(nameof(PacketError), typeof(PacketError));
        }

        /// <summary>
        /// 为序列化提供支持. Provide object for serialization
        /// </summary>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            info.AddValue(nameof(PacketError), _code);
            base.GetObjectData(info, context);
        }
    }
}
