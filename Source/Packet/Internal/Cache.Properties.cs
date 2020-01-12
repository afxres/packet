using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mikodev.Network.Internal
{
    internal partial class Cache
    {
        private static GetInfo InternalGetGetInfo(Type type)
        {
            if (type == typeof(object))
                goto fail;
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            if (properties.Length == 0)
                goto fail;

            var propertyList = new List<KeyValuePair<string, Type>>();
            var methodInfos = new List<MethodInfo>();
            for (var i = 0; i < properties.Length; i++)
            {
                var item = properties[i];
                var getter = item.GetGetMethod();
                if (getter == null)
                    continue;
                var parameters = getter.GetParameters();
                // Length != 0 -> indexer
                if (parameters == null || parameters.Length != 0)
                    continue;
                propertyList.Add(new KeyValuePair<string, Type>(item.Name, item.PropertyType));
                methodInfos.Add(getter);
            }

            var expressionList = new List<Expression>();
            var parameter = Expression.Parameter(typeof(object), "parameter");
            var objectArray = Expression.Parameter(typeof(object[]), "array");
            var value = Expression.Variable(type, "value");
            expressionList.Add(Expression.Assign(value, Expression.Convert(parameter, type)));

            for (var i = 0; i < propertyList.Count; i++)
            {
                var arrayAccess = Expression.ArrayAccess(objectArray, Expression.Constant(i));
                var result = Expression.Call(value, methodInfos[i]);
                var convert = Expression.Convert(result, typeof(object));
                var assign = Expression.Assign(arrayAccess, convert);
                expressionList.Add(assign);
            }

            var block = Expression.Block(new[] { value }, expressionList);
            var expression = Expression.Lambda<Action<object, object[]>>(block, parameter, objectArray);
            return new GetInfo(propertyList.ToArray(), expression.Compile());

        fail:
            throw PacketException.InvalidType(type);
        }

        private static SetInfo InternalGetSetInfoAnonymousType(Type type, PropertyInfo[] properties)
        {
            var constructorInfo = type.GetConstructors()?.FirstOrDefault(t => t.GetParameters().Select(x => x.ParameterType).SequenceEqual(properties.Select(x => x.PropertyType)));
            if (constructorInfo == null || constructorInfo.GetParameters().Select(x => x.Name.ToUpperInvariant()).SequenceEqual(properties.Select(x => x.Name.ToUpperInvariant())) == false)
                return null;

            var parameter = Expression.Parameter(typeof(object[]), "parameters");
            var expressionArray = new Expression[properties.Length];
            var propertyList = new KeyValuePair<string, Type>[properties.Length];
            for (var i = 0; i < properties.Length; i++)
            {
                var item = properties[i];
                var arrayIndex = Expression.ArrayIndex(parameter, Expression.Constant(i));
                var unbox = Expression.Convert(arrayIndex, item.PropertyType);
                expressionArray[i] = unbox;
                propertyList[i] = new KeyValuePair<string, Type>(item.Name, item.PropertyType);
            }

            // Reference type
            var instance = Expression.New(constructorInfo, expressionArray);
            var box = Expression.Convert(instance, typeof(object));
            var expression = Expression.Lambda<Func<object[], object>>(box, parameter);
            return new SetInfo(type, expression.Compile(), propertyList);
        }

        private static SetInfo InternalGetSetInfoProperties(Type type, PropertyInfo[] properties)
        {
            if (!type.IsValueType && type.GetConstructor(Type.EmptyTypes) == null)
                return null;
            var propertyList = new List<KeyValuePair<string, Type>>();
            var methodInfos = new List<MethodInfo>();

            for (var i = 0; i < properties.Length; i++)
            {
                var item = properties[i];
                var getter = item.GetGetMethod();
                var setter = item.GetSetMethod();
                if (getter == null || setter == null)
                    continue;
                var setterParameters = setter.GetParameters();
                if (setterParameters == null || setterParameters.Length != 1)
                    continue;
                propertyList.Add(new KeyValuePair<string, Type>(item.Name, item.PropertyType));
                methodInfos.Add(setter);
            }

            var expressionList = new List<Expression>();
            var parameter = Expression.Parameter(typeof(object[]), "parameters");
            var instance = Expression.Variable(type, "instance");

            expressionList.Add(Expression.Assign(instance, Expression.New(type)));
            for (var i = 0; i < propertyList.Count; i++)
            {
                var arrayIndex = Expression.ArrayIndex(parameter, Expression.Constant(i));
                var convert = Expression.Convert(arrayIndex, propertyList[i].Value);
                var setValue = Expression.Call(instance, methodInfos[i], convert);
                expressionList.Add(setValue);
            }
            expressionList.Add(Expression.Convert(instance, typeof(object)));

            var block = Expression.Block(new[] { instance }, expressionList);
            var expression = Expression.Lambda<Func<object[], object>>(block, parameter);
            return new SetInfo(type, expression.Compile(), propertyList.ToArray());
        }

        private static SetInfo InternalGetSetInfo(Type type)
        {
            if (type.IsAbstract || type.IsInterface)
                goto fail;
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            if (properties.Length == 0)
                goto fail;
            return InternalGetSetInfoAnonymousType(type, properties) ?? InternalGetSetInfoProperties(type, properties);
        fail:
            throw PacketException.InvalidType(type);
        }

        internal static GetInfo GetGetInfo(Type type)
        {
            return GetInfos.TryGetValue(type, out var info) ? info : GetInfos.GetOrAdd(type, InternalGetGetInfo(type));
        }

        internal static SetInfo GetSetInfo(Type type)
        {
            return SetInfos.TryGetValue(type, out var info) ? info : SetInfos.GetOrAdd(type, InternalGetSetInfo(type));
        }
    }
}
