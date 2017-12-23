using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;

namespace Mikodev.Network
{
    internal sealed class _DynamicReader : DynamicMetaObject
    {
        public _DynamicReader(Expression parameter, object value) : base(parameter, BindingRestrictions.Empty, value) { }

        /// <summary>
        /// Get node by key, throw if not found
        /// </summary>
        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            var rea = (PacketReader)Value;
            var val = rea._GetItem(binder.Name, false);
            var exp = Expression.Constant(val);
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        /// <summary>
        /// Cast node to target type
        /// </summary>
        public override DynamicMetaObject BindConvert(ConvertBinder binder)
        {
            var rea = (PacketReader)Value;
            var typ = binder.Type;
            if (rea._Convert(typ, out var val) == false)
                return base.BindConvert(binder);

            var exp = Expression.Constant(val);
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            var rea = (PacketReader)Value;
            if (rea._Init())
                return rea._dic.Keys;
            return base.GetDynamicMemberNames();
        }
    }
}
