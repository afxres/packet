using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;

namespace Mikodev.Binary
{
    internal sealed class DynamicToken : DynamicMetaObject
    {
        private static readonly Type[] assignableTypes;

        static DynamicToken()
        {
            var type = typeof(Token);
            var collection = new HashSet<Type>(type.GetInterfaces()) { type };
            while ((type = type.BaseType) != null)
                collection.Add(type);
            assignableTypes = collection.ToArray();
        }

        public DynamicToken(Expression parameter, object value) : base(parameter, BindingRestrictions.Empty, value) { }

        public override DynamicMetaObject BindConvert(ConvertBinder binder)
        {
            var value = Value;
            var type = binder.Type;
            if (Array.IndexOf(assignableTypes, type) < 0)
                value = ((Token)value).As(type);
            var constant = Expression.Constant(value, type);
            return new DynamicMetaObject(constant, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return ((Token)Value).GetTokens().Keys;
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            var value = ((Token)Value).GetTokens()[binder.Name];
            var constant = Expression.Constant(value);
            return new DynamicMetaObject(constant, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }
    }
}
