using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;

namespace Mikodev.Binary
{
    internal sealed class DynamicToken : DynamicMetaObject
    {
        private static readonly HashSet<Type> assignable;

        static DynamicToken()
        {
            var type = typeof(Token);
            var collection = new HashSet<Type>(type.GetInterfaces()) { type };
            while ((type = type.BaseType) != null)
                collection.Add(type);
            assignable = collection;
        }

        public DynamicToken(Expression parameter, object value) : base(parameter, BindingRestrictions.Empty, value) { }

        public override DynamicMetaObject BindConvert(ConvertBinder binder)
        {
            var value = Value;
            var type = binder.Type;
            if (!assignable.Contains(type))
                value = ((Token)value).As(type);
            var constant = Expression.Constant(value, type);
            return new DynamicMetaObject(constant, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            return ((Token)Value).Tokens.Keys;
        }

        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            var value = ((Token)Value).Tokens[binder.Name];
            var constant = Expression.Constant(value);
            return new DynamicMetaObject(constant, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }
    }
}
