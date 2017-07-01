using System.Dynamic;
using System.Linq.Expressions;

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
            var val = rdr._GetValue(binder.Name, true);
            if (val == null)
                throw new PacketException();
            var exp = Expression.Constant(val);
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        /// <summary>
        /// 动态转换元素 若无合适的转换方法则抛出异常
        /// </summary>
        public override DynamicMetaObject BindConvert(ConvertBinder binder)
        {
            var rdr = (PacketReader)Value;
            var fun = rdr._GetFunc(binder.Type);
            var val = fun.Invoke(rdr._buf, rdr._off, rdr._len);
            var exp = Expression.Constant(val);
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }
    }
}
