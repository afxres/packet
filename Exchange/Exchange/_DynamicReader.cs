using System.Collections.Generic;
using System.Dynamic;
using System.Linq.Expressions;

namespace Mikodev.Network
{
    internal sealed class _DynamicReader : DynamicMetaObject
    {
        public _DynamicReader(Expression parameter, object value) : base(parameter, BindingRestrictions.Empty, value) { }

        /// <summary>
        /// Get node by key, throw if not found
        /// </summary>
        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            var rea = (PacketReader)Value;
            var val = rea._Item(binder.Name, false);
            var exp = Expression.Constant(val);
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        /// <summary>
        /// Cast node to target type
        /// </summary>
        public override DynamicMetaObject BindConvert(ConvertBinder binder)
        {
            var rea = (PacketReader)Value;
            var spa = rea._spa;
            var typ = binder.Type;
            var val = default(object);
            var con = default(IPacketConverter);

            if ((con = _Caches.Converter(typ, rea._con, true)) != null)
                val = con._GetValueWrapErr(spa, true);
            else if (typ._IsEnumerableGeneric(out var inn))
                val = _Caches.Enumerable(inn).Invoke(rea);
            else return base.BindConvert(binder);

            var exp = Expression.Constant(val);
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        public override IEnumerable<string> GetDynamicMemberNames()
        {
            var rea = (PacketReader)Value;
            if (rea._Init())
                return rea._dic.Keys;
            return base.GetDynamicMemberNames();
        }
    }
}
