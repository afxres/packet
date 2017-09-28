using System.Dynamic;
using System.Linq.Expressions;

namespace Mikodev.Network
{
    internal sealed class _DynamicWriter : DynamicMetaObject
    {
        public _DynamicWriter(Expression parameter, object value) : base(parameter, BindingRestrictions.Empty, value) { }

        internal PacketWriter _Node(PacketWriter wtr, string key)
        {
            var dic = wtr._ItemList();
            if (dic.TryGetValue(key, out var res) && res is PacketWriter pkt)
                return pkt;
            var nod = new PacketWriter(wtr._con);
            dic.Add(key, nod);
            return nod;
        }

        /// <summary>
        /// Create node
        /// </summary>
        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            var val = _Node((PacketWriter)Value, binder.Name);
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
            if (PacketWriter._ItemNode(val, wtr._con, out var nod) == false)
                throw new PacketException(PacketError.TypeInvalid);
            var dic = wtr._ItemList();
            dic[key] = nod;
            var exp = Expression.Constant(val, typeof(object));
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }
    }
}
