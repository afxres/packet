using System.Dynamic;
using System.Linq.Expressions;

namespace Mikodev.Network
{
    internal class DynamicPacketWriter : DynamicMetaObject
    {
        internal DynamicPacketWriter(Expression parameter, object value) : base(parameter, BindingRestrictions.Empty, value) { }

        /// <summary>
        /// Create node
        /// </summary>
        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            var wtr = (PacketWriter)Value;
            var val = wtr._Item(binder.Name, null);
            var exp = Expression.Constant(val);
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        /// <summary>
        /// Set node, throw if type invalid
        /// </summary>
        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            var wtr = (PacketWriter)Value;
            var key = binder.Name;
            var val = value.Value;
            if (wtr._ItemVal(key, val) == false)
                throw new PacketException(PacketError.TypeInvalid);
            var nod = wtr._Item(key, null);
            var exp = Expression.Constant(val, typeof(object));
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }
    }
}
