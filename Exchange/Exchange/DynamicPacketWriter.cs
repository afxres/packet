using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using PushFunc = System.Func<object, byte[]>;

namespace Mikodev.Network
{
    internal class DynamicPacketWriter : DynamicMetaObject
    {
        internal DynamicPacketWriter(Expression parameter, object value) : base(parameter, BindingRestrictions.Empty, value) { }

        /// <summary>
        /// 动态创建节点
        /// </summary>
        public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
        {
            var wtr = (PacketWriter)Value;
            var val = wtr._Item(binder.Name);
            var exp = Expression.Constant(val);
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }

        /// <summary>
        /// 动态设置元素
        /// </summary>
        public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
        {
            var wtr = (PacketWriter)Value;
            var key = binder.Name;
            var val = value.Value;
            var typ = val.GetType();
            var fun = default(PushFunc);

            bool enumerable(out Type inn)
            {
                foreach (var i in typ.GetTypeInfo().GetInterfaces())
                {
                    if (i.IsGenericEnumerable())
                    {
                        var som = i.GetGenericArguments();
                        if (som.Length != 1)
                            continue;
                        inn = som[0];
                        return true;
                    }
                }
                inn = null;
                return false;
            }

            if (val is byte[] buf)
                wtr._Push(key, buf);
            else if (val is PacketWriter pkt)
                wtr._Item(key, pkt);
            else if ((fun = wtr._Func(typ, true)) != null)
                wtr._Push(key, fun.Invoke(val));
            else if (enumerable(out var inn))
                wtr.PushList(key, inn, (IEnumerable)val);
            else
                throw new PacketException(PacketErrorCode.InvalidType);

            var nod = wtr._Item(key);
            var exp = Expression.Constant(val, typeof(object));
            return new DynamicMetaObject(exp, BindingRestrictions.GetTypeRestriction(Expression, LimitType));
        }
    }
}
