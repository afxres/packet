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
            var dictionary = writer.GetDictionary();
            if (dictionary.TryGetValue(key, out var value))
                return value;
            var childWriter = new PacketWriter(writer.converters);
            dictionary[key] = childWriter;
            return childWriter;
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            var writer = GetItem((PacketWriter)Value, binder.Name);
            var constant = Expression.Constant(writer);
            return new DynamicMetaObject(constant, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject metaObject)
        {
            var writer = (PacketWriter)Value;
            var key = binder.Name;
            var value = metaObject.Value;
            var childWriter = PacketWriter.GetWriter(writer.converters, value, 0);
            var dictionary = writer.GetDictionary();
            dictionary[key] = childWriter;
            var constant = Expression.Constant(value, typeof(object));
            return new DynamicMetaObject(constant, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return ((PacketWriter)Value).GetKeys();
        }
    }
}
