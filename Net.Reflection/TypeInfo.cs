﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Net.Extensions;
namespace Net.Reflection
{
    public class TypeInfo
    {
        static ConcurrentDictionary<Type, TypeInfo> _objectInfos = new ConcurrentDictionary<Type, TypeInfo>();

        public Type Type { get; private set; }
        public TypeKind Kind { get; private set; }
        public TypeInfo ElementTypeInfo { get; private set; }
        public bool IsPrimitiveCollection => this.ElementTypeInfo != null && this.ElementTypeInfo.Kind == TypeKind.Primitive;
        private readonly Dictionary<string, TypePropertyInfo> _allProperties = new Dictionary<string, TypePropertyInfo>();


        public TypePropertyInfo GetPropertyByPath(string path)
        {
            var currentPath = path.TrimThenBy(".");
            var thenPath = path.TrimLeftBy(".").ToUpperFirstLetter();
            if (thenPath.IsEmpty() || thenPath == currentPath) return this[currentPath];
            var currentProp = this[currentPath];
            if (currentProp.IsNull()) return null;
            switch (currentProp.Kind)
            {
                case TypeKind.Unknown:
                    return null;
                case TypeKind.Primitive:
                    return null;
                case TypeKind.Complex:
                    return currentProp.Type.GetInfo().GetPropertyByPath(thenPath);
                case TypeKind.Collection:
                    return currentProp.ElementTypeInfo.GetPropertyByPath(thenPath);
                default:
                    return null;
            }
        }


        public IEnumerable<TypePropertyInfo> GetPropertiesByAttribute<T>()
            where T : Attribute
        => this._allProperties.Values.Where(p => p.HasAttribute<T>());
        public IEnumerable<TypePropertyInfo> GetAllProperties() => this._allProperties.Values.AsEnumerable();
        public TypePropertyInfo this[string propName]
        {
            get
            {
                if (!this.HasProperty(propName)) return null;
                return this._allProperties[propName];
            }
        }
        public bool HasProperty(string name) =>
            this._allProperties.ContainsKey(name);


        private TypeInfo()
        {

        }
        internal static TypeInfo GetTypeInfo(Type type, Dictionary<Type, TypeInfo> workingInfos = null)
        {
            if (_objectInfos.ContainsKey(type)) return _objectInfos[type];
            if (workingInfos != null && workingInfos.ContainsKey(type))
                return workingInfos[type];
            lock (type)
            {
                if (_objectInfos.ContainsKey(type)) return _objectInfos[type];
                workingInfos = workingInfos ?? new Dictionary<Type, TypeInfo>();
                var info = new TypeInfo();
                info.Type = type;
                workingInfos.Add(type, info);
                info.Kind = info.Type.GetTypeKind();
                if (info.Kind == TypeKind.Complex)
                    info.ParseProperties(workingInfos);
                else if (info.Kind == TypeKind.Collection)
                    info.ElementTypeInfo = GetTypeInfo(info.Type.GetCollectionElementType(), workingInfos);
                _objectInfos[type] = info;
                return info;
            }

        }



        private void ParseProperties(Dictionary<Type, TypeInfo> workingTypes)
        {
            foreach (var propInfo in this.Type.FindProperties())
            {
                this._allProperties[propInfo.Name] = TypePropertyInfo.Create(propInfo, workingTypes);
            }
        }

        public T GetAttribute<T>()
            where T : Attribute
        {
            return this.Type.GetCustomAttribute<T>();
        }
        public IEnumerable<T> GetAttributes<T>()
            where T : Attribute
        {
            return this.Type.GetCustomAttributes<T>();
        }
        public override string ToString()
            => this.Type.ToString();


    }
}