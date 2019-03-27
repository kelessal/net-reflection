﻿using Net.Extensions;
using Net.Reflection;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace Net.Reflection
{
    public static class TypeExtensions
    {
        static readonly char[] PROP_SPLITTER = new char[] { '.' };

        static ConcurrentDictionary<Type, MethodCollection> _genericMethods = new ConcurrentDictionary<Type, MethodCollection>();
        static readonly HashSet<Type> primitiveTypes = new HashSet<Type>(new[]
       {
            typeof(string),
            typeof(int),
            typeof(int?),
            typeof(double),
            typeof(double?),
            typeof(float),
            typeof(float?),
            typeof(decimal),
            typeof(decimal?),
            typeof(long),
            typeof(long?),
            typeof(Guid),
            typeof(Guid?),
            typeof(DateTime),
            typeof(DateTime?),
            typeof(bool),
            typeof(bool?),
            //typeof(TimeSpan),
            //typeof(TimeSpan?),
            typeof(byte)
        });
        public static Type GetGenericClosedTypeOf(this Type type, Type genericTypeDef)
        {
            if (!genericTypeDef.IsGenericTypeDefinition)
                throw new SystemException("It is not generic type  definition");
            var baseType = type.BaseType;
            if (baseType == typeof(object) || baseType == null) return null;
            if (baseType.IsGenericType && baseType.GetGenericTypeDefinition() == genericTypeDef)
                return baseType;
            return baseType.GetGenericClosedTypeOf(genericTypeDef);
        }
        public static string GetFriendlyName(this Type type)
        {
            if (type == typeof(int))
                return "int";
            else if (type == typeof(short))
                return "short";
            else if (type == typeof(byte))
                return "byte";
            else if (type == typeof(bool))
                return "bool";
            else if (type == typeof(long))
                return "long";
            else if (type == typeof(float))
                return "float";
            else if (type == typeof(double))
                return "double";
            else if (type == typeof(decimal))
                return "decimal";
            else if (type == typeof(string))
                return "string";
            else if (type.IsGenericType)
                return type.Name.Split('`')[0] + "<" + string.Join(", ", type.GetGenericArguments().Select(x => GetFriendlyName(x)).ToArray()) + ">";
            else
                return type.Name;
        }
        public static bool IsLogicalEqual(this object obj1,object obj2)
        {
            if (obj1 == null && obj2 == null) return true;
            if (obj1 == null || obj2 == null) return false;
            var info1 = obj1.GetType().GetInfo();
            var info2 = obj2.GetType().GetInfo();
            if (info1.Kind != info2.Kind) return false;
            if (info1.Kind == TypeKind.Unknown || info1.Kind == TypeKind.Primitive)
                return obj1.Equals(obj2);
            if (info1.Kind == TypeKind.Complex)
            {
                var anyDiff = info1.GetAllProperties()
                     .Where(p => info2.HasProperty(p.Name))
                     .Any(p => !p.GetValue(obj1).IsLogicalEqual(info2[p.Name].GetValue(obj2)));
                return !anyDiff;
            }
            // IEnumerable ise
            var values1 = new List<object>();
            var values2 = new List<object>();
            foreach (var item in obj1 as IEnumerable)
                values1.Add(item);
            foreach (var item in obj2 as IEnumerable)
                values2.Add(item);

            if (values1.Count != values2.Count) return false;
            for (int i = 0; i < values1.Count; i++)
                if (!values1[i].IsLogicalEqual(values2[i]))
                    return false;
            return true;
        }
        public static object GetDefault(Type type)
        {
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            return null;
        }
        public static object ChangeType(this object item,Type changeType)
        {
            try
            {
                if (changeType.IsEnum)
                    return Enum.Parse(changeType, item?.ToString());
                return Convert.ChangeType(item, changeType);
            }
            catch(Exception)
            {
                return null;
            }
        }
        public static PropertyInfo[] GetPropertyInfos(this Type type, string propName)
        {
            List<PropertyInfo> result = new List<PropertyInfo>();
            PropertyInfo curInfo = null;
            foreach (var name in propName.Split(PROP_SPLITTER, StringSplitOptions.RemoveEmptyEntries))
            {
                curInfo = curInfo == null ? type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) :
                    curInfo.PropertyType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (curInfo == null) return null;
                result.Add(curInfo);
            }
            return result.Count == 0 ? null : result.ToArray();
        }
        public static PropertyInfo GetSubProperty(this Type type, string propName)
        {
            PropertyInfo curInfo = null;
            foreach (var name in propName.Split(PROP_SPLITTER, StringSplitOptions.RemoveEmptyEntries))
            {
                curInfo = curInfo == null ? type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance) :
                    curInfo.PropertyType.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
                if (curInfo == null) return null;
            }
            return curInfo;
        }
        
        public static bool IsNullableOf(this Type nullableType,Type type)
            => nullableType.IsGenericType && Nullable.GetUnderlyingType(nullableType) == type;
        
        public static Type GetCollectionElementType(this Type type)
        {
            if (type.IsArray) return type.GetElementType();
            if (!type.IsGenericType) return null;
            if (type.GetGenericArguments().Length > 1) return null;
            return type.GetGenericArguments()[0];
        }

        public static Dictionary<string,object> ConvertToDictionary(this object obj,Func<TypePropertyInfo,bool> filter=null)
        {
            if (obj == null) return null;
            var result = new Dictionary<string, object>();
            Dictionary<string, object> nameValues = new Dictionary<string, object>();
            if (obj is Dictionary<string, object>)
                nameValues = obj as Dictionary<string, object>;
            else
            {
                var info = obj.GetType().GetInfo();
                if (info.Kind != TypeKind.Complex) return null;
                var props = info.GetAllProperties();
                if (filter != null)
                    props.Where(p => filter(p));
                nameValues = new Dictionary<string, object>();
                props.Foreach(p=>nameValues.Add(p.Name,p.GetValue(obj)));
            }
          
            foreach (var prop in nameValues)
            {
                var name = prop.Key;
                var value = prop.Value;
                if (value == null)
                {
                    result.Add(name, null);
                    continue;
                }
                var info = value.GetType().GetInfo();
                if(value is Dictionary<string, object>)
                {
                    result.Add(name, value.ConvertToDictionary(filter));
                }
                switch (info.Kind)
                {
                    case TypeKind.Unknown:
                    case TypeKind.Primitive:
                        result.Add(name, value);
                        continue;
                    case TypeKind.Complex:
                        result.Add(name, value.ConvertToDictionary(filter));
                        continue;
                    case TypeKind.Collection:
                        if (info.IsPrimitiveCollection) {
                            result.Add(name, value);
                            continue;
                        }
                        var list = new List<object>();
                        foreach(var subValue in value as IEnumerable)
                        {
                            list.Add(value.ConvertToDictionary(filter));
                        }
                        result.Add(name, list);
                        continue;
                }
            }

            return result;
        }
        
        public static MethodInfo FindMethod(this Type type, string methodName, Func<MethodInfo, bool> finder, params Type[] genericParameters)
        {
            if (_genericMethods.ContainsKey(type))
                return _genericMethods[type].SearchMethod(methodName, finder, genericParameters);
            var gmcollection = new MethodCollection(type);
            _genericMethods[type] = gmcollection;
            return gmcollection.SearchMethod(methodName, finder, genericParameters);

        }
        public static PropertyInfo[] FindProperties(this Type type)
        {
            if (type.IsInterface)
            {
                var propertyInfos = new List<PropertyInfo>();

                var considered = new List<Type>();
                var queue = new Queue<Type>();
                considered.Add(type);
                queue.Enqueue(type);
                while (queue.Count > 0)
                {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetInterfaces())
                    {
                        if (considered.Contains(subInterface)) continue;

                        considered.Add(subInterface);
                        queue.Enqueue(subInterface);
                    }

                    var typeProperties = subType.GetProperties(
                        BindingFlags.FlattenHierarchy
                        | BindingFlags.Public
                        | BindingFlags.Instance);

                    var newPropertyInfos = typeProperties
                        .Where(x => !propertyInfos.Contains(x));

                    propertyInfos.InsertRange(0, newPropertyInfos);
                }

                return propertyInfos.ToArray();
            }

            return type.GetProperties(BindingFlags.FlattenHierarchy
                | BindingFlags.Public | BindingFlags.Instance);

        }
        public static TypeKind GetTypeKind(this Type type)
        {
            if (type.Name.Contains("AnonymousType")) return TypeKind.Complex;
            if (type.IsPrimitiveType()) return TypeKind.Primitive;
            if (type.IsCollectionType()) return TypeKind.Collection;
            if (!type.IsGenericTypeDefinition  &&(type.IsClass || type.IsInterface)) return TypeKind.Complex;
            return TypeKind.Unknown;
        }
      
        public static bool IsPrimitiveType(this Type type)
            => primitiveTypes.Contains(type) || type.IsEnum;
        public static bool IsAssignableTo<T>(this Type type)
            => typeof(T).IsAssignableFrom(type);

      
        public static bool HasInterface(this Type type, Type interfaceType)
        {
            if (!interfaceType.IsInterface)
                throw new SystemException("It is not interface");
            return type.GetInterfaces()
                .Any(p => {
                    if (p == interfaceType) return true;
                    if (!interfaceType.IsGenericTypeDefinition) return false;
                    if (!p.IsGenericType) return false;
                    return p.GetGenericTypeDefinition() == interfaceType;
                });
        }
        public static bool IsCollectionType(this Type type)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(type)) return false;
            if (type.IsArray && type.GetArrayRank() == 1) return true;
            if (!type.IsGenericType) return false;
            if (type.GetGenericArguments().Length != 1) return false;
            return typeof(IEnumerable<>).MakeGenericType(type.GetCollectionElementType()).IsAssignableFrom(type);
        }

        public static IEnumerable<Type> GetBaseTypes(this Type type)
        {
            Type currentType = type.BaseType;
            while (currentType!=typeof(object))
            {
                yield return currentType;
                if (currentType == null) break;
                currentType = currentType.BaseType;
            }
        }
        public static TypeInfo GetInfo(this Type type)
        {
            return TypeInfo.GetTypeInfo(type);
        }


        public static void SetValue(this object item, string property, object value)
        {
            item.GetType().GetInfo()[property].SetValue(item, value);
        }
        public static T GetValue<T>(this object item, string property)
        {
            if(item is ExpandoObject)
            {
                var expandoItem = (IDictionary<string,object>) item;
                return expandoItem.ContainsKey(property) ? (T) expandoItem[property]  : default(T);
            }
            var info = item.GetType().GetInfo();
            if (!info.HasProperty(property)) return default(T);
            return info[property].GetValue<T>(item);
        }
        public static T GetPathValue<T>(this object item, string path)
        {
            if (item == null) return default(T);
            var left = path.TrimThenBy(".");
            var leftValue = item.GetValue<object>(left);
            if (leftValue == null) return default(T);
            if (left == path) return (T)leftValue;
            var right = path.TrimLeftBy(".");
            return leftValue.GetPathValue<T>(right);
        }
    }
}