using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using PacketWriterDictionary = System.Collections.Generic.Dictionary<string, Mikodev.Network.PacketWriter>;

namespace Mikodev.Network
{
    internal sealed class _DynamicWriter : DynamicMetaObject
    {
        public _DynamicWriter(Expression parameter, object value) : base(parameter, BindingRestrictions.Empty, value) { }

        private PacketWriter _GetItem(PacketWriter wtr, string key)
        {
            var lst = wtr.GetDictionary();
            if (lst.TryGetValue(key, out var res) && res is PacketWriter pkt)
                return pkt;
            var sub = new PacketWriter(wtr._cvt);
            lst[key] = sub;
            return sub;
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            var val = _GetItem((PacketWriter)Value, binder.Name);
            var exp = Expression.Constant(val);
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            var wtr = (PacketWriter)Value;
            var key = binder.Name;
            var val = value.Value;
            var sub = PacketWriter.GetWriter(wtr._cvt, val, 0);
            var lst = wtr.GetDictionary();
            lst[key] = sub;
            var exp = Expression.Constant(val, typeof(object));
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            var wtr = (PacketWriter)Value;
            if (wtr._itm is PacketWriterDictionary dic)
                return dic.Keys;
            return base.GetDynamicMemberNames();
        }
    }
}
