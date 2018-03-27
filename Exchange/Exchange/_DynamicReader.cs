using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;

namespace Mikodev.Network
{
    internal sealed class _DynamicReader : DynamicMetaObject
    {
        public _DynamicReader(Expression parameter, object value) : base(parameter, BindingRestrictions.Empty, value) { }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            var rea = (PacketReader)Value;
            var val = rea.GetItem(binder.Name, false);
            var exp = Expression.Constant(val);
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        public override DynamicMetaObject BindConvert(ConvertBinder binder)
        {
            var rea = (PacketReader)Value;
            var typ = binder.Type;
            var val = rea.GetValue(typ, 0);
            var exp = Expression.Constant(val);
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return ((PacketReader)Value).GetKeys();
        }
    }
}
