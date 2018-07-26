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
        private readonly struct ConverterGenerator
        {
            private static readonly string fsNamespace = "Microsoft.FSharp.Collections";
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
                        throw new InvalidOperationException($"Invalid dictionary key type : object");
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
                    throw new InvalidOperationException($"Circular type reference detected! type : {type}");
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
                        throw new InvalidOperationException("No valid F# toList method detected", ex);
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

            private Converter GenerateConverter(Type type)
            {
                if (reserveTypes.Contains(type))
                    throw new InvalidOperationException($"Invalid type : {type}");
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
                    throw new InvalidOperationException($"Multiple IEnumerable implementations, type : {type}");
                if (enumerableTypes.Length == 1)
                    return GenerateCollectionConverter(type, enumerableTypes[0]);
                // converter via properties
                var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                if (properties.Length == 0)
                    throw new InvalidOperationException($"No public property found, type : {type}");
                var toBytes = ToBytesDelegate(type, properties);
                var toValue = ToValueDelegate(type, properties, out var capacity);
                return (Converter)Activator.CreateInstance(typeof(ExpandoConverter<>).MakeGenericType(type), toBytes, toValue, capacity);
            }

            private Converter GenerateCollectionConverter(Type type, Type enumerableType)
            {
                var elementType = enumerableType.GetGenericArguments().Single();
                if (elementType == typeof(object))
                    throw new InvalidOperationException($"Invalid collection type : {type}");

                // dictionary or fs map
                if (elementType.IsGenericType && elementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    var elementTypes = elementType.GetGenericArguments();
                    var adapter = GetOrGenerateDictionaryAdapter(elementTypes);
                    // fs map
                    if (IsFSharpMap(type, out var constructorInfo))
                    {
                        var memory = Expression.Parameter(typeof(ReadOnlyMemory<byte>), "memory");
                        var instance = Expression.New(constructorInfo, Expression.Call(Expression.Constant(adapter), adapter.TupleDelegate.Method, memory));
                        var delegateType = typeof(Func<,>).MakeGenericType(typeof(ReadOnlyMemory<byte>), type);
                        var lambda = Expression.Lambda(delegateType, instance, memory);
                        return (Converter)Activator.CreateInstance(typeof(DelegateConverter<>).MakeGenericType(type), adapter.BytesDelegate, lambda.Compile(), 0);
                    }

                    var dictionaryType = typeof(Dictionary<,>).MakeGenericType(elementTypes);
                    if (!type.IsAssignableFrom(dictionaryType))
                        throw new InvalidOperationException($"Invalid key-value pair collection type : {type}");
                    // dictionary
                    return (Converter)Activator.CreateInstance(typeof(DelegateConverter<>).MakeGenericType(type), adapter.BytesDelegate, adapter.ValueDelegate, 0);
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

            #region tuple
            private Converter TupleConverter(Type type)
            {
                var name = type.Name;
                var elementCount = int.Parse(name.Substring(name.LastIndexOf('`') + 1));
                if ((uint)elementCount > 8)
                    throw new InvalidOperationException($"Invalid tuple type : {type}");
                var elementTypes = default(Type[]);
                var constructorInfo = type.GetConstructors().Single(r => (elementTypes = r.GetParameters().Select(x => x.ParameterType).ToArray()).Length == elementCount);
                var self = this;
                var converters = new Converter[elementCount];
                for (int i = 0; i < elementCount; i++)
                    converters[i] = GetOrGenerateConverter(elementTypes[i]);
                var length = converters.Any(r => r.Length == 0) ? 0 : converters.Sum(r => r.Length);
                var toBytes = TupleToBytesExpression(type, converters, length);
                var toValue = TupleToValueExpression(type, constructorInfo, elementTypes, converters);
                return (Converter)Activator.CreateInstance(typeof(DelegateConverter<>).MakeGenericType(type), toBytes.Compile(), toValue.Compile(), length);
            }

            private static LambdaExpression TupleToValueExpression(Type type, ConstructorInfo constructorInfo, Type[] elementTypes, Converter[] converters)
            {
                var memory = Expression.Parameter(typeof(ReadOnlyMemory<byte>), "memory");
                var vernier = Expression.Variable(typeof(Vernier), "vernier");

                var arguments = Enumerable.Range(0, converters.Length).Select(r => Expression.Variable(elementTypes[r], $"arg{r + 1}")).ToArray();
                var expressions = new List<Expression> { Expression.Assign(vernier, Expression.Convert(memory, typeof(Vernier))) };
                for (int i = 0; i < converters.Length; i++)
                {
                    var converter = converters[i];
                    expressions.Add(Expression.Call(vernier, Vernier.FlushExceptMethodInfo, Expression.Constant(converter.Length)));
                    expressions.Add(Expression.Assign(arguments[i], Expression.Call(Expression.Constant(converter), converter.ToValueDelegate.Method, Expression.Convert(vernier, typeof(ReadOnlyMemory<byte>)))));
                }
                expressions.Add(Expression.New(constructorInfo, arguments));
                var delegateType = typeof(Func<,>).MakeGenericType(typeof(ReadOnlyMemory<byte>), type);
                return Expression.Lambda(delegateType, Expression.Block(new[] { vernier }.Concat(arguments), expressions), memory);
            }

            private static LambdaExpression TupleToBytesExpression(Type type, Converter[] converters, int length)
            {
                var tuple = Expression.Parameter(type, "tuple");
                var allocator = Expression.Parameter(typeof(Allocator), "allocator");
                var offset = default(ParameterExpression);
                var stream = default(ParameterExpression);
                var variables = new List<ParameterExpression>();
                var expressions = new List<Expression>();
                var itemNames = Enumerable.Range(0, converters.Length).Take(7).Select(r => $"Item{r + 1}").ToList();
                if (converters.Length > 7)
                    itemNames.Add("Rest");
                var items = itemNames.Select(r => Expression.PropertyOrField(tuple, r)).ToArray();
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
                        expressions.Add(Expression.Assign(offset, Expression.Call(stream, UnsafeStream.AnchorExtendMethodInfo)));
                        expressions.Add(toBytesExpression);
                        expressions.Add(Expression.Call(stream, UnsafeStream.FinishExtendMethodInfo, offset));
                    }
                    else
                    {
                        expressions.Add(toBytesExpression);
                    }
                }
                var delegateType = typeof(Action<,>).MakeGenericType(typeof(Allocator), type);
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
                return type.IsAssignableFrom(setType) ? ((IDelegateConverter)GetOrGenerateConverter(setType)).ToValueFunction : null;
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
                if (converter.Length == 0)
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
                    list.Add(Expression.Call(stream, UnsafeStream.AppendExtendMethodInfo, Expression.Constant(buffer)));
                    var propertyValue = Expression.Call(instance, getMethod);
                    if (position == null)
                        variableList.Add(position = Expression.Variable(typeof(int), "position"));
                    list.Add(Expression.Assign(position, Expression.Call(stream, UnsafeStream.AnchorExtendMethodInfo)));
                    var converter = GetOrGenerateConverter(propertyType);
                    list.Add(Expression.Call(
                        Expression.Constant(converter),
                        converter.ToBytesDelegate.Method,
                        allocator, propertyValue));
                    list.Add(Expression.Call(stream, UnsafeStream.FinishExtendMethodInfo, position));
                }
                var memory = Expression.Block(variableList, list);
                var delegateType = typeof(Action<,>).MakeGenericType(typeof(Allocator), type);
                var expression = Expression.Lambda(delegateType, memory, allocator, instance);
                return expression.Compile();
            }

            private Delegate ToValueDelegate(Type type, PropertyInfo[] properties, out int capacity)
            {
                capacity = 0;
                if (type.IsAbstract || type.IsInterface)
                    return null;
                var delegateType = typeof(Func<,>).MakeGenericType(typeof(Dictionary<string, ReadOnlyMemory<byte>>), type);
                var constructorInfo = type.GetConstructor(Type.EmptyTypes);
                return type.IsValueType || constructorInfo != null
                    ? ToValueDelegateProperties(type, delegateType, properties, ref capacity)
                    : ToValueDelegateAnonymousType(type, delegateType, properties, ref capacity);
            }

            private Delegate ToValueDelegateAnonymousType(Type type, Type delegateType, PropertyInfo[] properties, ref int capacity)
            {
                // anonymous type or record
                var constructors = type.GetConstructors();
                if (constructors.Length != 1)
                    return null;

                var constructorInfo = constructors[0];
                var constructorParameters = constructorInfo.GetParameters();
                if (properties.Length != constructorParameters.Length)
                    return null;

                for (int i = 0; i < properties.Length; i++)
                    if (properties[i].Name != constructorParameters[i].Name || properties[i].PropertyType != constructorParameters[i].ParameterType)
                        return null;

                var dictionary = Expression.Parameter(typeof(Dictionary<string, ReadOnlyMemory<byte>>), "dictionary");
                var expressionArray = new Expression[properties.Length];
                for (int i = 0; i < properties.Length; i++)
                {
                    var item = properties[i];
                    var converter = GetOrGenerateConverter(item.PropertyType);
                    var memory = Expression.Property(dictionary, "Item", Expression.Constant(item.Name));
                    var value = Expression.Call(Expression.Constant(converter), converter.ToValueDelegate.Method, memory);
                    expressionArray[i] = value;
                }
                var lambda = Expression.Lambda(delegateType, Expression.New(constructorInfo, expressionArray), dictionary);
                capacity = properties.Length;
                return lambda.Compile();
            }

            private Delegate ToValueDelegateProperties(Type type, Type delegateType, PropertyInfo[] properties, ref int capacity)
            {
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
                var dictionary = Expression.Parameter(typeof(Dictionary<string, ReadOnlyMemory<byte>>), "dictionary");
                var instance = Expression.Variable(type, "instance");
                var expressionList = new List<Expression> { Expression.Assign(instance, Expression.New(type)) };
                foreach (var item in propertyList)
                {
                    var converter = GetOrGenerateConverter(item.PropertyType);
                    var memory = Expression.Property(dictionary, "Item", Expression.Constant(item.Name));
                    var value = Expression.Call(Expression.Constant(converter), converter.ToValueDelegate.Method, memory);
                    expressionList.Add(Expression.Call(instance, item.GetSetMethod(), value));
                }
                expressionList.Add(instance);
                var lambda = Expression.Lambda(delegateType, Expression.Block(new[] { instance }, expressionList), dictionary);
                capacity = propertyList.Count;
                return lambda.Compile();
            }
            #endregion
        }
    }
}
