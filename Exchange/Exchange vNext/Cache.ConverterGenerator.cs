using Mikodev.Binary.Converters;
using Mikodev.Binary.RuntimeConverters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mikodev.Binary
{
    public partial class Cache
    {
        private readonly struct ConverterGenerator
        {
            private const int TupleMaximumItems = 8;

            private const int TupleMinimumItems = 1;

            private static readonly string fsNamespace = "Microsoft.FSharp.Collections";

            private static readonly MethodInfo sliceMethodInfo = typeof(ReadOnlyMemory<byte>).GetMethod(nameof(ReadOnlyMemory<byte>.Slice), new[] { typeof(int), typeof(int) });

            private static MethodInfo fsToListMethodInfo;

            private readonly Cache cache;

            private readonly HashSet<Type> types;

            private ConverterGenerator(Cache cache)
            {
                this.cache = cache;
                types = new HashSet<Type>();
            }

            internal static Converter GenerateConverter(Cache cache, Type type)
            {
                var generator = new ConverterGenerator(cache);
                var converter = generator.GetOrGenerateConverter(type);
                return converter;
            }

            private static MethodCallExpression MakeDelegateCall(Delegate functor, params Expression[] arguments)
            {
                var method = functor.Method;
                var instance = Expression.Constant(functor.Target);
                return Expression.Call(instance, method, arguments);
            }

            #region get or generate
            private byte[] GetOrCache(string key)
            {
                var texts = cache.texts;
                if (!texts.TryGetValue(key, out var bytes))
                    texts.TryAdd(key, (bytes = Converter.Encoding.GetBytes(key)));
                return bytes;
            }

            private DictionaryAdapter GetOrGenerateDictionaryAdapter(params Type[] elementTypes)
            {
                var adapters = cache.adapters;
                var adapterType = typeof(DictionaryAdapter<,>).MakeGenericType(elementTypes);
                if (!adapters.TryGetValue(adapterType, out var adapter))
                {
                    if (elementTypes[0] == typeof(object))
                        throw new InvalidOperationException($"Invalid dictionary key type: {typeof(object)}");
                    var keyConverter = GetOrGenerateConverter(elementTypes[0]);
                    var valueConverter = GetOrGenerateConverter(elementTypes[1]);
                    adapter = (DictionaryAdapter)Activator.CreateInstance(adapterType, keyConverter, valueConverter);
                    adapters.TryAdd(adapterType, adapter);
                }
                return adapter;
            }

            private Converter GetOrGenerateConverter(Type type)
            {
                var converters = cache.converters;
                if (converters.TryGetValue(type, out var converter))
                    return converter;
                if (!types.Add(type))
                    throw new InvalidOperationException($"Circular type reference detected! type: {type}");
                converter = GenerateConverter(type);
                converters.TryAdd(type, converter);
                return converter;
            }
            #endregion

            #region is ...
            private bool IsTuple(Type type)
            {
                if (type.Namespace != "System")
                    return false;
                var name = type.Name;
                return type.IsValueType ? name.StartsWith("ValueTuple`") : name.StartsWith("Tuple`");
            }

            private bool IsFSharpList(Type type)
            {
                if (type.Name != "FSharpList`1" || type.Namespace != fsNamespace)
                    return false;
                var methodInfo = fsToListMethodInfo;
                if (methodInfo == null)
                {
                    try
                    {
                        var moduleType = type.Assembly.GetType($"{fsNamespace}.SeqModule", true);
                        var methodInfos = moduleType.GetMethods();
                        methodInfo = methodInfos.Where(r => r.Name == "ToList").Single();
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("No valid F# 'ToList' method detected", ex);
                    }
                }
                fsToListMethodInfo = methodInfo;
                return true;
            }

            private static bool IsFSharpMap(Type type, out ConstructorInfo constructorInfo)
            {
                if (type.Name != "FSharpMap`2" || type.Namespace != fsNamespace)
                    goto fail;
                var genericArguments = type.GetGenericArguments();
                var enumerableType = typeof(IEnumerable<>).MakeGenericType(typeof(Tuple<,>).MakeGenericType(genericArguments));
                constructorInfo = type.GetConstructor(new[] { enumerableType });
                return constructorInfo != null;

            fail:
                constructorInfo = null;
                return false;
            }
            #endregion

            #region generate
            private Converter GenerateConverter(Type type)
            {
                if (type.Assembly == typeof(Cache).Assembly)
                    throw new InvalidOperationException($"Invalid type: {type}");
                // enum
                if (type.IsEnum)
                    return (Converter)Activator.CreateInstance(typeof(UnmanagedValueConverter<>).MakeGenericType(type));
                // tuple
                if (IsTuple(type))
                    return TupleConverter(type);

                var definition = type.IsGenericType ? type.GetGenericTypeDefinition() : null;
                if (definition == typeof(KeyValuePair<,>))
                    throw new InvalidOperationException($"Use tuple instead of key-value pair");

                // collection
                var interfaces = type.IsInterface ? type.GetInterfaces().Concat(new[] { type }).ToArray() : type.GetInterfaces();
                var enumerableTypes = interfaces.Where(r => r.IsGenericType && r.GetGenericTypeDefinition() == typeof(IEnumerable<>)).ToArray();
                if (enumerableTypes.Length > 1)
                    throw new InvalidOperationException($"Multiple IEnumerable implementations detected, type: {type}");
                if (enumerableTypes.Length == 1)
                    return GenerateCollectionConverter(type, enumerableTypes[0]);
                // converter via properties
                var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(x => x.GetGetMethod()?.GetParameters().Length == 0)
                    .ToArray();
                if (properties.Length == 0)
                    throw new InvalidOperationException($"No available property found, type: {type}");
                var toBytes = ToBytesDelegate(type, properties);
                var toValue = ToValueDelegate(type, properties);
                return (Converter)Activator.CreateInstance(typeof(ExpandoConverter<>).MakeGenericType(type), toBytes, toValue, properties.Length);
            }

            private Converter GenerateCollectionConverter(Type type, Type enumerableType)
            {
                var elementType = enumerableType.GetGenericArguments().Single();
                if (elementType == typeof(object))
                    throw new InvalidOperationException($"Invalid collection type: {type}");

                // dictionary or fs map
                if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    var elementTypes = elementType.GetGenericArguments();
                    var adapter = GetOrGenerateDictionaryAdapter(elementTypes);
                    // fs map
                    if (IsFSharpMap(type, out var constructorInfo))
                    {
                        var memory = Expression.Parameter(typeof(ReadOnlyMemory<byte>), "memory");
                        var instance = Expression.New(constructorInfo, MakeDelegateCall(adapter.GetToTupleDelegate(), memory));
                        var delegateType = typeof(ToValue<>).MakeGenericType(type);
                        var lambda = Expression.Lambda(delegateType, instance, memory);
                        return (Converter)Activator.CreateInstance(typeof(DelegateConverter<>).MakeGenericType(type), adapter.GetToBytesDelegate(), lambda.Compile(), 0);
                    }

                    var dictionaryType = typeof(Dictionary<,>).MakeGenericType(elementTypes);
                    if (!type.IsAssignableFrom(dictionaryType))
                        throw new InvalidOperationException($"Invalid key-value pair collection type: {type}");
                    // dictionary
                    return (Converter)Activator.CreateInstance(typeof(DelegateConverter<>).MakeGenericType(type), adapter.GetToBytesDelegate(), adapter.GetToValueDelegate(), 0);
                }

                // list
                var converter = GetOrGenerateConverter(elementType);
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
                    return (Converter)Activator.CreateInstance(typeof(ListConverter<>).MakeGenericType(elementType), converter, GetOrGenerateConverter(elementType.MakeArrayType()));
                // array
                if (type.IsArray)
                {
                    if (type.GetArrayRank() != 1)
                        throw new NotSupportedException("Multidimensional arrays are not supported, use array of arrays instead.");
                    // enum array or else
                    return elementType.IsEnum
                        ? (Converter)Activator.CreateInstance(typeof(UnmanagedArrayConverter<>).MakeGenericType(elementType))
                        : (Converter)Activator.CreateInstance(typeof(ArrayConverter<>).MakeGenericType(elementType), converter);
                }

                // fs list
                var converterType = typeof(EnumerableConverter<,>).MakeGenericType(type, elementType);
                if (IsFSharpList(type))
                {
                    var enumerable = Expression.Parameter(enumerableType, "enumerable");
                    var instance = Expression.Call(fsToListMethodInfo.MakeGenericMethod(elementType), enumerable);
                    var delegateType = typeof(Func<,>).MakeGenericType(enumerableType, type);
                    var lambda = Expression.Lambda(delegateType, instance, enumerable);
                    return (Converter)Activator.CreateInstance(converterType, converter, lambda.Compile());
                }
                // other collections
                return (Converter)Activator.CreateInstance(converterType, converter, ToCollectionDelegate(type, enumerableType, elementType));
            }
            #endregion

            #region tuple
            private Converter TupleConverter(Type type)
            {
                var name = type.Name;
                var elementCount = int.Parse(name.Substring(name.LastIndexOf('`') + 1));
                if (elementCount < TupleMinimumItems || elementCount > TupleMaximumItems)
                    throw new InvalidOperationException($"Invalid tuple type: {type}");
                var elementTypes = default(Type[]);
                var constructorInfo = type.GetConstructors().Single(r => (elementTypes = r.GetParameters().Select(x => x.ParameterType).ToArray()).Length == elementCount);
                var converters = new Converter[elementCount];
                for (var i = 0; i < elementCount; i++)
                    converters[i] = GetOrGenerateConverter(elementTypes[i]);
                var definitions = converters.Select(x => x.length).ToArray();
                var length = definitions.Any(x => x == 0) ? 0 : definitions.Sum();
                var toBytes = TupleToBytesExpression(type, converters, length);
                var toValue = TupleToValueExpression(type, constructorInfo, converters);
                return (Converter)Activator.CreateInstance(typeof(FixedConverter<>).MakeGenericType(type), toBytes.Compile(), toValue.Compile(), length);
            }

            private static LambdaExpression TupleToValueExpression(Type type, ConstructorInfo constructorInfo, Converter[] converters)
            {
                var memory = Expression.Parameter(typeof(ReadOnlyMemory<byte>), "memory");
                var vernier = Expression.Parameter(typeof(Vernier), "vernier");
                var expressions = new List<Expression>();
                var variables = new ParameterExpression[converters.Length];
                for (var i = 0; i < converters.Length; i++)
                {
                    var converter = converters[i];
                    var variable = Expression.Variable(converter.GetValueType(), $"item{i + 1}");
                    variables[i] = variable;
                    var update = Expression.Call(vernier, Vernier.UpdateExceptMethodInfo, Expression.Constant(converter.length, typeof(int)));
                    var offset = Expression.Field(vernier, Vernier.OffsetFieldInfo);
                    var length = Expression.Field(vernier, Vernier.LengthFieldInfo);
                    var invoke = Expression.Call(memory, sliceMethodInfo, offset, length);
                    var assign = Expression.Assign(variable, MakeDelegateCall(converter.GetToValueDelegate(), invoke));
                    expressions.Add(update);
                    expressions.Add(assign);
                }
                expressions.Add(Expression.New(constructorInfo, variables));
                var delegateType = typeof(ToValueFixed<>).MakeGenericType(type);
                return Expression.Lambda(delegateType, Expression.Block(variables, expressions), memory, vernier);
            }

            private static LambdaExpression TupleToBytesExpression(Type type, Converter[] converters, int length)
            {
                var tuple = Expression.Parameter(type, "tuple");
                var allocator = Expression.Parameter(typeof(Allocator), "allocator");
                var offset = default(ParameterExpression);
                var variables = new List<ParameterExpression>();
                var expressions = new List<Expression>();
                var itemNames = Enumerable.Range(0, converters.Length).Take(TupleMaximumItems - 1).Select(i => $"Item{i + 1}").ToList();
                if (converters.Length == TupleMaximumItems)
                    itemNames.Add("Rest");
                var items = itemNames.Select(r => Expression.PropertyOrField(tuple, r)).ToArray();
                if (length == 0)
                    variables.Add(offset = Expression.Variable(typeof(int), "offset"));
                for (var i = 0; i < converters.Length; i++)
                {
                    var converter = converters[i];
                    var expression = converter.length == 0
                        ? Expression.Call(allocator, Allocator.AppendValueExtendMethodInfo.MakeGenericMethod(converter.GetValueType()), Expression.Constant(converter), items[i])
                        : MakeDelegateCall(converter.GetToBytesDelegate(), allocator, items[i]);
                    expressions.Add(expression);
                }
                var delegateType = typeof(ToBytes<>).MakeGenericType(type);
                return Expression.Lambda(delegateType, Expression.Block(variables, expressions), allocator, tuple);
            }
            #endregion

            #region collection
            private Delegate ToCollectionDelegate(Type type, Type enumerableType, Type elementType)
            {
                var delegateType = typeof(Func<,>).MakeGenericType(enumerableType, type);
                if (type.IsAbstract == false && type.IsInterface == false)
                {
                    var enumerable = Expression.Parameter(enumerableType, "enumerable");
                    var constructorInfo = type.GetConstructor(new[] { enumerableType });
                    // .ctor(IEnumerable<T>)
                    if (constructorInfo != null)
                    {
                        var instance = Expression.New(constructorInfo, enumerable);
                        var lambda = Expression.Lambda(delegateType, instance, enumerable);
                        return lambda.Compile();
                    }
                    // Add(T)
                    var addMethodInfo = type.GetMethod("Add", new[] { elementType });
                    if (addMethodInfo != null && (type.IsValueType || (constructorInfo = type.GetConstructor(Type.EmptyTypes)) != null))
                        return ToCollectionDelegateAddMethod(type, elementType, enumerable, delegateType, addMethodInfo);
                }

                // find default implementation
                var arrayType = elementType.MakeArrayType();
                var listType = typeof(List<>).MakeGenericType(elementType);
                if (type.IsAssignableFrom(arrayType) && type.IsAssignableFrom(listType))
                {
                    var source = Expression.Parameter(typeof(object), "source");
                    var lambda = Expression.Lambda(delegateType, Expression.Convert(source, type), source);
                    return lambda.Compile();
                }
                var setType = typeof(HashSet<>).MakeGenericType(elementType);
                return type.IsAssignableFrom(setType) ? ((IEnumerableConverter)GetOrGenerateConverter(setType)).GetToEnumerableDelegate() : null;
            }

            private Delegate ToCollectionDelegateAddMethod(Type type, Type elementType, ParameterExpression enumerable, Type delegateType, MethodInfo addMethodInfo)
            {
                var instance = Expression.Variable(type, "collection");
                var index = Expression.Variable(typeof(int), "index");
                var label = Expression.Label(type, "label");

                var variableList = new List<ParameterExpression> { instance, index };
                var expressionList = new List<Expression>
                {
                    Expression.Assign(index, Expression.Constant(0)),
                    Expression.Assign(instance, Expression.New(type)),
                };

                var converter = GetOrGenerateConverter(elementType);
                if (converter.length == 0)
                {
                    var listType = typeof(List<>).MakeGenericType(elementType);
                    var list = Expression.Variable(listType, "list");
                    variableList.Add(list);
                    expressionList.Add(Expression.Assign(list, Expression.Convert(enumerable, listType)));
                    expressionList.Add(Expression.Loop(
                        Expression.IfThenElse(
                            Expression.LessThan(index, Expression.Property(list, "Count")),
                            Expression.Call(instance, addMethodInfo, Expression.Property(list, "Item", Expression.PostIncrementAssign(index))),
                            Expression.Break(label, instance)),
                        label));
                }
                else
                {
                    var arrayType = elementType.MakeArrayType();
                    var array = Expression.Variable(arrayType, "array");
                    variableList.Add(array);
                    expressionList.Add(Expression.Assign(array, Expression.Convert(enumerable, arrayType)));
                    expressionList.Add(Expression.Loop(
                        Expression.IfThenElse(
                            Expression.LessThan(index, Expression.ArrayLength(array)),
                            Expression.Call(instance, addMethodInfo, Expression.ArrayAccess(array, Expression.PostIncrementAssign(index))),
                            Expression.Break(label, instance)),
                        label));
                }
                var lambda = Expression.Lambda(delegateType, Expression.Block(variableList, expressionList), enumerable);
                return lambda.Compile();
            }
            #endregion

            #region anonymous type or via properties
            private Delegate ToBytesDelegate(Type type, PropertyInfo[] properties)
            {
                var instance = Expression.Parameter(type, "instance");
                var allocator = Expression.Parameter(typeof(Allocator), "allocator");
                var position = Expression.Variable(typeof(int), "position");
                var list = new List<Expression>();
                foreach (var property in properties)
                {
                    var propertyType = property.PropertyType;
                    var propertyValue = Expression.Property(instance, property);
                    var buffer = GetOrCache(property.Name);
                    var converter = GetOrGenerateConverter(propertyType);
                    list.Add(Expression.Call(allocator, Allocator.AppendBytesExtendMethodInfo, Expression.Constant(buffer)));
                    list.Add(Expression.Call(allocator, Allocator.AppendValueExtendMethodInfo.MakeGenericMethod(propertyType), Expression.Constant(converter), propertyValue));
                }
                var memory = Expression.Block(new[] { position }, list);
                var delegateType = typeof(ToBytes<>).MakeGenericType(type);
                var expression = Expression.Lambda(delegateType, memory, allocator, instance);
                return expression.Compile();
            }

            private Delegate ToValueDelegate(Type type, PropertyInfo[] properties)
            {
                if (type.IsAbstract || type.IsInterface)
                    return null;
                var delegateType = typeof(Func<,>).MakeGenericType(typeof(Dictionary<string, ReadOnlyMemory<byte>>), type);
                var constructorInfo = type.GetConstructor(Type.EmptyTypes);
                return type.IsValueType || constructorInfo != null
                    ? ToValueDelegateProperties(type, delegateType, properties)
                    : ToValueDelegateAnonymousType(type, delegateType, properties);
            }

            private Delegate ToValueDelegateAnonymousType(Type type, Type delegateType, PropertyInfo[] properties)
            {
                // anonymous type or record
                var constructors = type.GetConstructors();
                if (constructors.Length != 1)
                    return null;

                var constructorInfo = constructors[0];
                var constructorParameters = constructorInfo.GetParameters();
                if (properties.Length != constructorParameters.Length)
                    return null;

                for (var i = 0; i < properties.Length; i++)
                    if (properties[i].Name != constructorParameters[i].Name || properties[i].PropertyType != constructorParameters[i].ParameterType)
                        return null;

                var dictionary = Expression.Parameter(typeof(Dictionary<string, ReadOnlyMemory<byte>>), "dictionary");
                var expressionArray = new Expression[properties.Length];
                for (var i = 0; i < properties.Length; i++)
                {
                    var item = properties[i];
                    var converter = GetOrGenerateConverter(item.PropertyType);
                    var memory = Expression.Property(dictionary, "Item", Expression.Constant(item.Name));
                    expressionArray[i] = MakeDelegateCall(converter.GetToValueDelegate(), memory);
                }
                var lambda = Expression.Lambda(delegateType, Expression.New(constructorInfo, expressionArray), dictionary);
                return lambda.Compile();
            }

            private Delegate ToValueDelegateProperties(Type type, Type delegateType, PropertyInfo[] properties)
            {
                var dictionary = Expression.Parameter(typeof(Dictionary<string, ReadOnlyMemory<byte>>), "dictionary");
                var instance = Expression.Variable(type, "instance");
                var expressionList = new List<Expression> { Expression.Assign(instance, Expression.New(type)) };
                foreach (var property in properties)
                {
                    if (property.GetSetMethod() == null)
                        throw new InvalidOperationException($"Property '{property.Name}' does not have a public setter, type: {type}");
                    var converter = GetOrGenerateConverter(property.PropertyType);
                    var memory = Expression.Property(dictionary, "Item", Expression.Constant(property.Name));
                    var expression = MakeDelegateCall(converter.GetToValueDelegate(), memory);
                    expressionList.Add(Expression.Assign(Expression.Property(instance, property), expression));
                }
                expressionList.Add(instance);
                var lambda = Expression.Lambda(delegateType, Expression.Block(new[] { instance }, expressionList), dictionary);
                return lambda.Compile();
            }
            #endregion
        }
    }
}
