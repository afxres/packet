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
            var val = rea._GetItem(binder.Name, false);
            var exp = Expression.Constant(val);
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        public override DynamicMetaObject BindConvert(ConvertBinder binder)
        {
            var rea = (PacketReader)Value;
            var typ = binder.Type;
            var val = rea._GetValue(typ, 0);
            var exp = Expression.Constant(val);
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            var rea = (PacketReader)Value;
            var dic = rea._GetItemDictionary();
            if (dic != null)
                return dic.Keys;
            return base.GetDynamicMemberNames();
        }
    }
}
