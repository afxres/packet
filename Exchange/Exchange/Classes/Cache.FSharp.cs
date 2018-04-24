using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mikodev.Network
{
    partial class Cache
    {
        private static MethodInfo s_to_fslist;

        private static bool IsFSharpList(Type type)
        {
            if (type.Name != "FSharpList`1" || type.Namespace != "Microsoft.FSharp.Collections")
                return false;
            var fun = s_to_fslist;
            if (fun != null)
                return true;
            var mod = type.Assembly.GetType("Microsoft.FSharp.Collections.ArrayModule", false, false);
            if (mod == null)
                return false;
            var met = mod.GetMethods().Where(r => r.Name == "ToList").ToArray();
            if (met.Length != 1)
                return false;
            s_to_fslist = met[0];
            return true;
        }

        private static Func<PacketReader, IPacketConverter, object> GetToFSharpListFunction(Type elementType)
        {
            var con = Expression.Parameter(typeof(IPacketConverter), "converter");
            var rea = Expression.Parameter(typeof(PacketReader), "reader");
            var met = s_to_array.MakeGenericMethod(elementType);
            var cal = Expression.Call(met, rea, con);
            var cvt = s_to_fslist.MakeGenericMethod(elementType);
            var inv = Expression.Call(cvt, cal);
            var box = Expression.Convert(inv, typeof(object));
            var exp = Expression.Lambda<Func<PacketReader, IPacketConverter, object>>(box, rea, con);
            var fun = exp.Compile();
            return fun;
        }

        private static Func<object[], object> GetCastFSharpListFunction(Type elementType)
        {
            var blk = GetCastArrayExpression(elementType, out var arr);
            var cvt = s_to_fslist.MakeGenericMethod(elementType);
            var inv = Expression.Call(cvt, blk);
            var box = Expression.Convert(inv, typeof(object));
            var exp = Expression.Lambda<Func<object[], object>>(box, arr);
            var fun = exp.Compile();
            return fun;
        }
    }
}
