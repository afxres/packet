using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using PullFunc = System.Func<byte[], int, int, object>;

namespace Mikodev.Network
{
    internal class DynamicPacketReader : DynamicMetaObject
    {
        internal DynamicPacketReader(Expression parameter, object value) : base(parameter, BindingRestrictions.Empty, value) { }

        /// <summary>
        /// 动态获取元素 若元素不存在则抛出异常
        /// </summary>
        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            var rdr = (PacketReader)Value;
            var val = rdr._Item(binder.Name);
            var exp = Expression.Constant(val);
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        /// <summary>
        /// 动态转换元素 若无合适的转换方法则抛出异常
        /// </summary>
        public override DynamicMetaObject BindConvert(ConvertBinder binder)
        {
            var rdr = (PacketReader)Value;
            var typ = binder.Type;
            var val = default(object);
            var fun = default(PullFunc);

            object enumerator()
            {
                var arg = typ.GetGenericArguments();
                if (arg.Length != 1)
                    throw new PacketException(PacketErrorCode.InvalidType);
                var ret = typeof(PacketReader).GetTypeInfo().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance);
                foreach (var i in ret)
                {
                    if (i.Name.Equals(nameof(PacketReader._ListGeneric)) == false)
                        continue;
                    return i.MakeGenericMethod(arg[0]).Invoke(rdr, new object[] { false });
                }
                throw new PacketException(PacketErrorCode.None);
            }

            if (typ == typeof(byte[]))
                val = rdr._buf.Split(rdr._off, rdr._len);
            else if ((fun = rdr._Func(typ, true)) != null)
                val = fun.Invoke(rdr._buf, rdr._off, rdr._len);
            else if (typ.IsGenericEnumerable())
                val = enumerator();
            else
                throw new PacketException(PacketErrorCode.InvalidType);

            var exp = Expression.Constant(val);
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }
    }
}
