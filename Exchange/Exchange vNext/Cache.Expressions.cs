using Mikodev.Binary.Converters;
using Mikodev.Binary.RuntimeConverters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mikodev.Binary
{
    partial class Cache
    {
        private byte[] GetOrCache(string key)
        {
            if (!encodingCache.TryGetValue(key, out var bytes))
                encodingCache.TryAdd(key, (bytes = Converter.Encoding.GetBytes(key)));
            return bytes;
        }

        private bool IsTuple(Type type)
        {
            if (type.IsAbstract || type.IsGenericType == false || type.Namespace != "System")
                return false;
            var name = type.Name;
            return type.IsValueType ? name.StartsWith("ValueTuple`") : name.StartsWith("Tuple`");
        }

        private Converter CreateConverter(Type type)
        {
            // enum
            if (type.IsEnum)
                return (Converter)Activator.CreateInstance(typeof(UnmanagedValueConverter<>).MakeGenericType(type));

            if (type.IsArray)
            {
                // array
                if (type.GetArrayRank() != 1)
                    throw new NotSupportedException("Multidimensional arrays are not supported, use array of arrays instead.");
                var elementType = type.GetElementType();
                if (elementType == typeof(object))
                    goto fail;
                // enum array
                if (elementType.IsEnum)
                    return (Converter)Activator.CreateInstance(typeof(UnmanagedArrayConverter<>).MakeGenericType(elementType));
                var converter = GetOrCreateConverter(elementType);
                return (Converter)Activator.CreateInstance(typeof(ArrayConverter<>).MakeGenericType(elementType), converter);
            }

            var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
            if (definition == typeof(Dictionary<,>) || definition == typeof(IDictionary<,>))
            {
                // dictionary
                var elementTypes = type.GetGenericArguments();
                if (elementTypes[0] == typeof(object))
                    goto fail;
                var keyConverter = GetOrCreateConverter(elementTypes[0]);
                var valueConverter = GetOrCreateConverter(elementTypes[1]);
                var converterDefinition = definition == typeof(Dictionary<,>) ? typeof(DictionaryConverter<,>) : typeof(DictionaryInterfaceConverter<,>);
                return (Converter)Activator.CreateInstance(converterDefinition.MakeGenericType(elementTypes), keyConverter, valueConverter);
            }

            if (definition == typeof(KeyValuePair<,>))
                throw new InvalidOperationException("Use tuple instead of key-value pair");
            // tuple
            if (IsTuple(type))
                return TupleConverter(type);

            if (definition == typeof(List<>))
            {
                var elementType = type.GetGenericArguments().Single();
                if (elementType == typeof(object))
                    goto fail;
                var converter = GetOrCreateConverter(elementType);
                return (Converter)Activator.CreateInstance(typeof(ListConverter<>).MakeGenericType(elementType), converter);
            }

            var interfaces = type.IsInterface ? type.GetInterfaces().Concat(new[] { type }).ToArray() : type.GetInterfaces();
            var enumerable = interfaces.Where(r => r.IsGenericType && r.GetGenericTypeDefinition() == typeof(IEnumerable<>)).ToArray();
            if (enumerable.Length > 1)
                goto fail;
            if (enumerable.Length == 1)
            {
                // collection
                var elementType = enumerable[0].GetGenericArguments().Single();
                if (elementType == typeof(object))
                    goto fail;
                var converter = GetOrCreateConverter(elementType);
                var converterType = typeof(EnumerableConverter<,>).MakeGenericType(type, elementType);
                return (Converter)Activator.CreateInstance(converterType, converter, ToCollectionDelegate(type, elementType) ?? ToCollectionDelegateImplementation(type, elementType));
            }
            return (Converter)Activator.CreateInstance(typeof(ExpandoConverter<>).MakeGenericType(type), ToBytesDelegate(type), ToValueDelegate(type, out var capacity), capacity);

            fail:
            throw new InvalidOperationException($"Invalid collection type: {type}");
        }

        #region tuple
        private Converter TupleConverter(Type type)
        {
            var name = type.Name;
            var elementCount = int.Parse(name.Substring(name.LastIndexOf('`') + 1));
            var elementTypes = default(Type[]);
            var constructorInfo = type.GetConstructors().Single(r => (elementTypes = r.GetParameters().Select(x => x.ParameterType).ToArray()).Length == elementCount);
            var converters = elementTypes.Select(r => GetOrCreateConverter(r)).ToArray();
            var length = converters.Any(r => r.Length == 0) ? 0 : converters.Sum(r => r.Length);

            var toBytes = TupleToBytesExpression(type, converters, length);
            var toValue = TupleToValueExpression(constructorInfo, elementTypes, converters);
            return (Converter)Activator.CreateInstance(typeof(DelegateConverter<>).MakeGenericType(type), toBytes.Compile(), toValue.Compile(), length);
        }

        private static LambdaExpression TupleToValueExpression(ConstructorInfo constructorInfo, Type[] elementTypes, Converter[] converters)
        {
            var block = Expression.Parameter(typeof(Block), "block");
            var vernier = Expression.Variable(typeof(Vernier), "vernier");
            var parameters = Enumerable.Range(0, converters.Length).Select(r => Expression.Variable(elementTypes[r], "item" + (r + 1))).ToArray();
            var expressions = new List<Expression> { Expression.Assign(vernier, Expression.Convert(block, typeof(Vernier))) };
            for (int i = 0; i < converters.Length; i++)
            {
                var converter = converters[i];
                expressions.Add(Expression.Call(vernier, Vernier.FlushExceptMethodInfo, Expression.Constant(converter.Length)));
                expressions.Add(Expression.Assign(parameters[i], Expression.Call(Expression.Constant(converter), converter.ToValueDelegate.Method, Expression.Convert(vernier, typeof(Block)))));
            }
            expressions.Add(Expression.New(constructorInfo, parameters));
            var toValue = Expression.Lambda(Expression.Block(new[] { vernier }.Concat(parameters), expressions), block);
            return toValue;
        }

        private static LambdaExpression TupleToBytesExpression(Type type, Converter[] converters, int length)
        {
            var tuple = Expression.Parameter(type, "tuple");
            var allocator = Expression.Parameter(typeof(Allocator), "allocator");
            var offset = default(ParameterExpression);
            var stream = default(ParameterExpression);
            var variables = new List<ParameterExpression>();
            var expressions = new List<Expression>();
            var items = Enumerable.Range(0, converters.Length).Select(r => Expression.PropertyOrField(tuple, "Item" + (r + 1))).ToArray();
            if (length == 0)
            {
                variables.Add(offset = Expression.Variable(typeof(int), "offset"));
                variables.Add(stream = Expression.Variable(typeof(UnsafeStream), "stream"));
                expressions.Add(Expression.Assign(stream, Expression.Field(allocator, Allocator.FieldInfo)));
            }
            for (int i = 0; i < converters.Length; i++)
            {
                var converter = converters[i];
                var toBytesExpression = Expression.Call(Expression.Constant(converter), converter.ToBytesDelegate.Method, allocator, items[i]);
                if (converter.Length == 0)
                {
                    expressions.Add(Expression.Assign(offset, Expression.Call(stream, UnsafeStream.BeginModifyMethodInfo)));
                    expressions.Add(toBytesExpression);
                    expressions.Add(Expression.Call(stream, UnsafeStream.EndModifyMethodInfo, offset));
                }
                else
                {
                    expressions.Add(toBytesExpression);
                }
            }
            return Expression.Lambda(Expression.Block(variables, expressions), allocator, tuple);
        }
        #endregion

        private Delegate ToCollectionDelegate(Type type, Type elementType)
        {
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = Expression.Parameter(listType, "list");
            var lambda = default(LambdaExpression);
            if (type.IsAssignableFrom(listType))
            {
                lambda = Expression.Lambda(Expression.Convert(list, type), list);
            }
            else
            {
                var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
                var constructorInfo = type.GetConstructor(new[] { enumerableType });
                if (constructorInfo == null)
                    return null;
                lambda = Expression.Lambda(Expression.New(constructorInfo, list), list);
            }
            return lambda.Compile();
        }

        private Delegate ToCollectionDelegateImplementation(Type type, Type elementType)
        {
            // ISet<T> 的默认实现采用 HashSet<T>
            var collectionType = typeof(HashSet<>).MakeGenericType(elementType);
            return type.IsAssignableFrom(collectionType) && GetOrCreateConverter(collectionType) is IDelegateConverter enumerableConverter
                ? enumerableConverter.ToValueFunction
                : null;
        }

        #region anonymous type or via properties
        private Delegate ToBytesDelegate(Type type)
        {
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var instance = Expression.Parameter(type, "instance");
            var allocator = Expression.Parameter(typeof(Allocator), "allocator");
            var stream = Expression.Variable(typeof(UnsafeStream), "stream");
            var position = default(ParameterExpression);
            var variableList = new List<ParameterExpression> { stream };
            var list = new List<Expression> { Expression.Assign(stream, Expression.Field(allocator, Allocator.FieldInfo)) };
            foreach (var i in properties)
            {
                var getMethod = i.GetGetMethod();
                if (getMethod == null || getMethod.GetParameters().Length != 0)
                    continue;
                var propertyType = i.PropertyType;
                var buffer = GetOrCache(i.Name);
                list.Add(Expression.Call(stream, UnsafeStream.WriteExtendMethodInfo, Expression.Constant(buffer)));
                var propertyValue = Expression.Call(instance, getMethod);
                if (position == null)
                    variableList.Add(position = Expression.Variable(typeof(int), "position"));
                list.Add(Expression.Assign(position, Expression.Call(stream, UnsafeStream.BeginModifyMethodInfo)));
                var converter = GetOrCreateConverter(propertyType);
                list.Add(Expression.Call(
                    Expression.Constant(converter),
                    converter.ToBytesDelegate.Method,
                    allocator, propertyValue));
                list.Add(Expression.Call(stream, UnsafeStream.EndModifyMethodInfo, position));
            }
            var block = Expression.Block(variableList, list);
            var expression = Expression.Lambda(block, allocator, instance);
            return expression.Compile();
        }

        private Delegate ToValueDelegate(Type type, out int capacity)
        {
            if (type.IsValueType)
                return ToValueDelegateProperties(type, out capacity);
            var constructorInfo = type.GetConstructor(Type.EmptyTypes);
            return constructorInfo != null ? ToValueDelegateProperties(type, out capacity) : ToValueDelegateAnonymous(type, out capacity);
        }

        private Delegate ToValueDelegateAnonymous(Type type, out int capacity)
        {
            var constructors = type.GetConstructors();
            if (constructors.Length != 1)
                goto fail;

            var constructorInfo = constructors[0];
            var constructorParameters = constructorInfo.GetParameters();
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            if (properties.Length != constructorParameters.Length)
                goto fail;

            for (int i = 0; i < properties.Length; i++)
                if (properties[i].Name != constructorParameters[i].Name || properties[i].PropertyType != constructorParameters[i].ParameterType)
                    goto fail;

            var dictionary = Expression.Parameter(typeof(Dictionary<string, Block>), "dictionary");
            var indexerMethod = typeof(Dictionary<string, Block>).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Single(r => r.PropertyType == typeof(Block) && r.GetIndexParameters().Select(x => x.ParameterType).SequenceEqual(new[] { typeof(string) }))
                .GetGetMethod();
            var expressionArray = new Expression[properties.Length];
            for (int i = 0; i < properties.Length; i++)
            {
                var current = properties[i];
                var converter = GetOrCreateConverter(current.PropertyType);
                var block = Expression.Call(dictionary, indexerMethod, Expression.Constant(current.Name));
                var value = Expression.Call(Expression.Constant(converter), converter.ToValueDelegate.Method, block);
                expressionArray[i] = value;
            }
            var lambda = Expression.Lambda(Expression.New(constructorInfo, expressionArray), dictionary);
            capacity = properties.Length;
            return lambda.Compile();

            fail:
            capacity = 0;
            return null;
        }

        private Delegate ToValueDelegateProperties(Type type, out int capacity)
        {
            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var propertyList = new List<PropertyInfo>();
            for (int i = 0; i < properties.Length; i++)
            {
                var current = properties[i];
                var getter = current.GetGetMethod();
                var setter = current.GetSetMethod();
                if (getter == null || setter == null)
                    continue;
                var setterParameters = setter.GetParameters();
                if (setterParameters == null || setterParameters.Length != 1)
                    continue;
                propertyList.Add(current);
            }
            var dictionary = Expression.Parameter(typeof(Dictionary<string, Block>), "dictionary");
            var indexerMethod = typeof(Dictionary<string, Block>).GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Single(r => r.PropertyType == typeof(Block) && r.GetIndexParameters().Select(x => x.ParameterType).SequenceEqual(new[] { typeof(string) }))
                .GetGetMethod();
            var instance = Expression.Variable(type, "instance");
            var expressionList = new List<Expression> { Expression.Assign(instance, Expression.New(type)) };
            foreach (var i in propertyList)
            {
                var converter = GetOrCreateConverter(i.PropertyType);
                var block = Expression.Call(dictionary, indexerMethod, Expression.Constant(i.Name));
                var value = Expression.Call(Expression.Constant(converter), converter.ToValueDelegate.Method, block);
                expressionList.Add(Expression.Call(instance, i.GetSetMethod(), value));
            }
            expressionList.Add(instance);
            var lambda = Expression.Lambda(Expression.Block(new[] { instance }, expressionList), dictionary);
            capacity = propertyList.Count;
            return lambda.Compile();
        }
        #endregion
    }
}
