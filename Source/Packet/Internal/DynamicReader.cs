using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace Mikodev.Network
{
    internal sealed class DynamicReader : DynamicMetaObject
    {
        private static readonly MethodInfo GetDynamicResultMethodInfo = typeof(DynamicReader).GetMethod(nameof(GetDynamicResult), BindingFlags.Static | BindingFlags.NonPublic);

        private static readonly MethodInfo GetDynamicMemberMethodInfo = typeof(DynamicReader).GetMethod(nameof(GetDynamicMember), BindingFlags.Static | BindingFlags.NonPublic);

        private static object GetDynamicMember(object instance, string key)
        {
            return ((PacketReader)instance).GetReader(key, false);
        }

        private static object GetDynamicResult(object instance, Type type)
        {
            return ((PacketReader)instance).GetValue(type, level: 0);
        }

        public DynamicReader(Expression parameter, object value) : base(parameter, BindingRestrictions.Empty, value) { }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            var invoke = Expression.Call(GetDynamicMemberMethodInfo, this.Expression, Expression.Constant(binder.Name));
            return new DynamicMetaObject(invoke, BindingRestrictions.GetTypeRestriction(this.Expression, this.LimitType));
        }

        public override DynamicMetaObject BindConvert(ConvertBinder binder)
        {
            var invoke = Expression.Call(GetDynamicResultMethodInfo, this.Expression, Expression.Constant(binder.Type));
            var result = Expression.Convert(invoke, binder.Type);
            return new DynamicMetaObject(result, BindingRestrictions.GetTypeRestriction(this.Expression, this.LimitType));
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return ((PacketReader)this.Value).Keys;
        }
    }
}
