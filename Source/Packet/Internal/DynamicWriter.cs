using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace Mikodev.Network
{
    internal sealed class DynamicWriter : DynamicMetaObject
    {
        private static readonly MethodInfo GetDynamicMemberMethodInfo = typeof(DynamicWriter).GetMethod(nameof(GetDynamicMember), BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo SetDynamicMemberMethodInfo = typeof(DynamicWriter).GetMethod(nameof(SetDynamicMember), BindingFlags.Static | BindingFlags.NonPublic);

        private static object GetDynamicMember(object instance, string key)
        {
            var writer = (PacketWriter)instance;
            var dictionary = writer.GetDictionary();
            if (dictionary.TryGetValue(key, out var value))
                return value;
            var childWriter = new PacketWriter(writer.converters);
            dictionary[key] = childWriter;
            return childWriter;
        }

        private static object SetDynamicMember(object instance, string key, object item)
        {
            var writer = (PacketWriter)instance;
            var childWriter = PacketWriter.GetWriter(writer.converters, item, 0);
            var dictionary = writer.GetDictionary();
            dictionary[key] = childWriter;
            return item;
        }

        public DynamicWriter(Expression parameter, object value) : base(parameter, BindingRestrictions.Empty, value) { }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            var invoke = Expression.Call(GetDynamicMemberMethodInfo, this.Expression, Expression.Constant(binder.Name));
            return new DynamicMetaObject(invoke, BindingRestrictions.GetTypeRestriction(this.Expression, this.LimitType));
        }

        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject metaObject)
        {
            var source = Expression.Convert(metaObject.Expression, typeof(object));
            var invoke = Expression.Call(SetDynamicMemberMethodInfo, this.Expression, Expression.Constant(binder.Name), source);
            return new DynamicMetaObject(invoke, BindingRestrictions.GetTypeRestriction(this.Expression, this.LimitType));
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return ((PacketWriter)this.Value).GetKeys();
        }
    }
}
