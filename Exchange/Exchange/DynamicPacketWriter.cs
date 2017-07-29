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
            var key = binder.Name;

            PacketWriter node()
            {
                var dic = wtr._ItemList();
                if (dic.TryGetValue(key, out var res))
                    return res;
                var nod = new PacketWriter(wtr._con);
                dic.Add(key, nod);
                return nod;
            }

            var val = node();
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
            var nod = PacketWriter._ItemNode(val, wtr._con);
            if (nod == null)
                throw new PacketException(PacketError.TypeInvalid);
            wtr._ItemPush(key, nod);
            var exp = Expression.Constant(val, typeof(object));
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }
    }
}
