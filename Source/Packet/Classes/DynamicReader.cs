using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;

namespace Mikodev.Network
{
    internal sealed class DynamicReader : DynamicMetaObject
    {
        public DynamicReader(Expression parameter, object value) : base(parameter, BindingRestrictions.Empty, value) { }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            var reader = (PacketReader)Value;
            var value = reader.GetItem(binder.Name, false);
            var constant = Expression.Constant(value);
            return new DynamicMetaObject(constant, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        public override DynamicMetaObject BindConvert(ConvertBinder binder)
        {
            var reader = (PacketReader)Value;
            var type = binder.Type;
            var value = reader.GetValue(type, 0);
            var constant = Expression.Constant(value, type);
            return new DynamicMetaObject(constant, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return ((PacketReader)Value).Keys;
        }
    }
}
