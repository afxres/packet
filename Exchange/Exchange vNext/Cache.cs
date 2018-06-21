using Mikodev.Binary.Converters;
using Mikodev.Binary.RuntimeConverters;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Mikodev.Binary
{
    public sealed class Cache
    {
        #region static
        private static readonly IReadOnlyList<Converter> defaultConverters;

        static Cache()
        {
            var unmanagedTypes = new[]
            {
                typeof(bool),
                typeof(byte),
                typeof(sbyte),
                typeof(char),
                typeof(short),
                typeof(int),
                typeof(long),
                typeof(ushort),
                typeof(uint),
                typeof(ulong),
                typeof(float),
                typeof(double),
            };
            var valueConverters = unmanagedTypes.Select(r => (Converter)Activator.CreateInstance(typeof(UnmanagedValueConverter<>).MakeGenericType(r)));
            var arrayConverters = unmanagedTypes.Select(r => (Converter)Activator.CreateInstance(typeof(UnmanagedArrayConverter<>).MakeGenericType(r)));
            var converters = new List<Converter>();
            converters.AddRange(valueConverters);
            converters.AddRange(arrayConverters);
            converters.Add(new StringConverter());
            converters.Add(new DateTimeConverter());
            converters.Add(new TimeSpanConverter());
            converters.Add(new GuidConverter());
            defaultConverters = converters;
        }
        #endregion

        private readonly ConcurrentDictionary<Type, Converter> converters;
        private readonly ConcurrentDictionary<string, byte[]> encodingCache = new ConcurrentDictionary<string, byte[]>();

        public Cache(IEnumerable<Converter> converters = null)
        {
            var dictionary = new ConcurrentDictionary<Type, Converter>();
            if (converters != null)
                foreach (var i in converters)
                    dictionary.TryAdd(i.ValueType, i);
            foreach (var i in defaultConverters)
                dictionary.TryAdd(i.ValueType, i);
            dictionary[typeof(object)] = new ObjectConverter(this);
            this.converters = dictionary;
        }

        #region reflection
        private byte[] GetOrCache(string key)
        {
            if (!encodingCache.TryGetValue(key, out var bytes))
                encodingCache.TryAdd(key, (bytes = Converter.Encoding.GetBytes(key)));
            return bytes;
        }

        internal Converter GetOrCreateConverter(Type type)
        {
            if (!converters.TryGetValue(type, out var converter))
                converters.TryAdd(type, (converter = CreateConverter(type)));
            return converter;
        }

        private Converter CreateConverter(Type type)
        {
            // enum
            if (type.IsEnum)
                return (Converter)Activator.CreateInstance(typeof(UnmanagedValueConverter<>).MakeGenericType(type));

            if (type.IsArray)
            {
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
                var elementTypes = type.GetGenericArguments();
                if (elementTypes[0] == typeof(object))
                    goto fail;
                var keyConverter = GetOrCreateConverter(elementTypes[0]);
                var valueConverter = GetOrCreateConverter(elementTypes[1]);
                var converterDefinition = definition == typeof(Dictionary<,>) ? typeof(DictionaryConverter<,>) : typeof(IDictionaryConverter<,>);
                return (Converter)Activator.CreateInstance(converterDefinition.MakeGenericType(elementTypes), keyConverter, valueConverter);
            }

            if (definition == typeof(KeyValuePair<,>))
                throw new InvalidOperationException();

            #region tuple
            var typeName = type.Name;
            if (type.Namespace == "System" && type.IsValueType ? typeName.StartsWith("ValueTuple`") : !type.IsAbstract && typeName.StartsWith("Tuple`"))
            {
                var tupleTypeCount = int.Parse(typeName.Substring(typeName.LastIndexOf('`') + 1));
                var tupleTypes = default(Type[]);
                var tupleConstructor = type.GetConstructors().Single(r => (tupleTypes = r.GetParameters().Select(x => x.ParameterType).ToArray()).Length == tupleTypeCount);
                var converters = tupleTypes.Select(r => GetOrCreateConverter(r)).ToArray();
                var tupleLength = converters.Any(r => r.Length == 0) ? 0 : converters.Sum(r => r.Length);

                #region bytes
                var allocator = Expression.Parameter(typeof(Allocator), "allocator");
                var tuple = Expression.Parameter(type, "tuple");
                var offset = default(ParameterExpression);
                var stream = default(ParameterExpression);
                var bytesVariables = new List<ParameterExpression>();
                var bytesExpressions = new List<Expression>();
                var bytesItems = Enumerable.Range(0, tupleTypeCount).Select(r => Expression.PropertyOrField(tuple, "Item" + (r + 1))).ToArray();
                if (tupleLength == 0)
                {
                    bytesVariables.Add(offset = Expression.Variable(typeof(int), "offset"));
                    bytesVariables.Add(stream = Expression.Variable(typeof(UnsafeStream), "stream"));
                    bytesExpressions.Add(Expression.Assign(stream, Expression.Field(allocator, Allocator.FieldInfo)));
                }
                for (int i = 0; i < tupleTypeCount; i++)
                {
                    var converter = converters[i];
                    var toBytesExpression = Expression.Call(Expression.Constant(converter), converter.ToBytesDelegate.Method, allocator, bytesItems[i]);
                    if (converter.Length == 0)
                    {
                        bytesExpressions.Add(Expression.Assign(offset, Expression.Call(stream, UnsafeStream.BeginModifyMethodInfo)));
                        bytesExpressions.Add(toBytesExpression);
                        bytesExpressions.Add(Expression.Call(stream, UnsafeStream.EndModifyMethodInfo, offset));
                    }
                    else
                    {
                        bytesExpressions.Add(toBytesExpression);
                    }
                }
                var bytesLambda = Expression.Lambda(Expression.Block(bytesVariables, bytesExpressions), allocator, tuple);
                #endregion

                #region value
                var block = Expression.Parameter(typeof(Block), "block");
                var vernier = Expression.Variable(typeof(Vernier), "vernier");
                var parameters = Enumerable.Range(0, tupleTypeCount).Select(r => Expression.Variable(tupleTypes[r], "item" + (r + 1))).ToArray();
                var valueExpressions = new List<Expression> { Expression.Assign(vernier, Expression.Convert(block, typeof(Vernier))) };
                for (int i = 0; i < tupleTypeCount; i++)
                {
                    var converter = converters[i];
                    valueExpressions.Add(Expression.Call(vernier, Vernier.FlushExceptMethodInfo, Expression.Constant(converter.Length)));
                    valueExpressions.Add(Expression.Assign(parameters[i], Expression.Call(Expression.Constant(converter), converter.ToValueDelegate.Method, Expression.Convert(vernier, typeof(Block)))));
                }
                valueExpressions.Add(Expression.New(tupleConstructor, parameters));
                var valueLambda = Expression.Lambda(Expression.Block(new[] { vernier }.Concat(parameters), valueExpressions), block);
                #endregion
                return (Converter)Activator.CreateInstance(typeof(DelegateConverter<>).MakeGenericType(type), bytesLambda.Compile(), valueLambda.Compile(), tupleLength);
            }
            #endregion

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
                var elementType = enumerable[0].GetGenericArguments().Single();
                if (elementType == typeof(object))
                    goto fail;
                var converter = GetOrCreateConverter(elementType);
                return (Converter)Activator.CreateInstance(typeof(EnumerableConverter<,>).MakeGenericType(type, elementType), converter, ToCollectionDelegate(type, elementType) ?? ToCollectionDelegateImplementation(type, elementType));
            }

            return (Converter)Activator.CreateInstance(typeof(ExpandoConverter<>).MakeGenericType(type), ToBytesDelegate(type), ToValueDelegate(type, out var capacity), capacity);
            fail:
            throw new InvalidOperationException($"Invalid collection type: {type}");
        }

        private Delegate ToCollectionDelegate(Type type, Type elementType)
        {
            var listType = typeof(List<>).MakeGenericType(elementType);
            var list = Expression.Parameter(listType, "collection");
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
            return type.IsAssignableFrom(collectionType) && GetOrCreateConverter(collectionType) is IEnumerableConverter enumerableConverter
                ? enumerableConverter.ToValueFunction
                : null;
        }

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

        private Delegate ToValueDelegate(Type type, out int capacity)
        {
            if (type.IsValueType)
                return ToValueDelegateProperties(type, out capacity);
            var constructorInfo = type.GetConstructor(Type.EmptyTypes);
            return constructorInfo != null ? ToValueDelegateProperties(type, out capacity) : ToValueDelegateAnonymous(type, out capacity);
        }
        #endregion

        #region export
        private T Deserialize<T>(Block block)
        {
            var converter = (Converter<T>)GetOrCreateConverter(typeof(T));
            var value = converter.ToValue(block);
            return value;
        }

        private object Deserialize(Type type, Block block)
        {
            var converter = GetOrCreateConverter(type);
            var value = converter.ToValueNonGeneric(block);
            return value;
        }

        public T Deserialize<T>(byte[] buffer) => Deserialize<T>(new Block(buffer));

        public T Deserialize<T>(byte[] buffer, T anonymous) => Deserialize<T>(buffer);

        public T Deserialize<T>(byte[] buffer, int offset, int length) => Deserialize<T>(new Block(buffer, offset, length));

        public T Deserialize<T>(byte[] buffer, int offset, int length, T anonymous) => Deserialize<T>(buffer, offset, length);

        public object Deserialize(byte[] buffer, Type type) => Deserialize(type, new Block(buffer));

        public object Deserialize(byte[] buffer, int offset, int length, Type type) => Deserialize(type, new Block(buffer, offset, length));

        public byte[] Serialize<T>(T value)
        {
            var converter = (Converter<T>)GetOrCreateConverter(typeof(T));
            var stream = new UnsafeStream();
            var allocator = new Allocator(stream);
            converter.ToBytes(allocator, value);
            return stream.GetBytes();
        }

        public byte[] Serialize(object value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            var converter = GetOrCreateConverter(value.GetType());
            var stream = new UnsafeStream();
            var allocator = new Allocator(stream);
            converter.ToBytesNonGeneric(allocator, value);
            return stream.GetBytes();
        }

        public Token NewToken(byte[] buffer) => new Token(this, new Block(buffer));

        public Token NewToken(byte[] buffer, int offset, int length) => new Token(this, new Block(buffer, offset, length));
        #endregion
    }
}
