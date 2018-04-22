using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;

namespace Mikodev.Network
{
    internal sealed class DynamicWriter : DynamicMetaObject
    {
        public DynamicWriter(Expression parameter, object value) : base(parameter, BindingRestrictions.Empty, value) { }

        private PacketWriter GetItem(PacketWriter writer, string key)
        {
            var lst = writer.GetDictionary();
            if (lst.TryGetValue(key, out var res) && res is PacketWriter pkt)
                return pkt;
            var sub = new PacketWriter(writer.converters);
            lst[key] = sub;
            return sub;
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            var val = GetItem((PacketWriter)Value, binder.Name);
            var exp = Expression.Constant(val);
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            var wtr = (PacketWriter)Value;
            var key = binder.Name;
            var val = value.Value;
            var sub = PacketWriter.GetWriter(wtr.converters, val, 0);
            var lst = wtr.GetDictionary();
            lst[key] = sub;
            var exp = Expression.Constant(val, typeof(object));
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return ((PacketWriter)Value).GetKeys();
        }
    }
}
