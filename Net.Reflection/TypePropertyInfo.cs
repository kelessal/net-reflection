using Net.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Net.Reflection
{
    public sealed class TypePropertyInfo
    {
        readonly Dictionary<Type,List<Attribute>> _attributes = new  Dictionary<Type,List<Attribute>>();
        readonly PropertyInfo _property;
        Func<object, object> _getter;
        Action<object,object> _setter;
        private TypeInfo _propertyTypeInfo;
        public Type Type => this._property.PropertyType;
        public TypeKind Kind => this._propertyTypeInfo.Kind;

        public bool IsTypeOf(Type type)
        {
            if (this.Type == type) return true;
            if (this.Kind != TypeKind.Collection) return false;
            if (this.ElementTypeInfo.Type == type) return true;
            return false;
        }
        public TypeInfo ElementTypeInfo => this._propertyTypeInfo.ElementTypeInfo;
        public bool IsPrimitiveCollection => this._propertyTypeInfo.IsPrimitiveCollection;
        private TypePropertyInfo(PropertyInfo property)
        {
            this._property = property;
            
        }
        private void ParseAttributes()
        {
            foreach (var attr in _property.GetCustomAttributes())
            {
                var attrType = attr.GetType();
                if (!_attributes.ContainsKey(attrType))
                    _attributes.Add(attrType, new List<Attribute>());
                _attributes[attrType].Add(attr);
            }
        }
        public bool HasAttribute<T>()
            where T:Attribute
        {
            return _attributes.ContainsKey(typeof(T));
        }
        public T GetAttribute<T>()
            where T:Attribute
        {
            if (!this.HasAttribute<T>()) return null;
            return (T) this._attributes[typeof(T)][0];
        }
        public IEnumerable<T> GetAttributes<T>()
            where T : Attribute
        {
            if (!this.HasAttribute<T>())
                yield break;
            foreach (var attr in _attributes[typeof(T)])
                yield  return (T) attr;
        }
        public IEnumerable<T> GetAllAttributes<T>()
            where T:Attribute
        {
            if (this._attributes.ContainsKey(typeof(T))) yield break;
            foreach (var item in this._attributes[typeof(T)])
                yield return (T) item;
        }
        public IEnumerable<Attribute> GetAllAttributes()
        {
            return this._attributes.Values.SelectMany(p => p);
        }
        public string Name => this._property.Name;
        public PropertyInfo Raw => this._property;

        public object GetValue(object obj)
        {
            if (_getter != null) return _getter(obj);
            lock (this)
            {
                if (_getter != null)
                    return _getter(obj);
                _getter = PropertyExpressionBuilder.CreateGetterFunc(this.Raw.DeclaringType, this.Name);
            }
            return _getter(obj);
        }
        public void SetValue(object obj, Object value)
        {
            
            if (!_property.CanWrite) return;
            if (_setter != null) _setter(obj,value);
            lock (this)
            {
                if (_setter.IsNull())
                    _setter = PropertyExpressionBuilder.CreateSetterFunc(this.Raw.DeclaringType, this.Name);
            }
            _setter(obj,value);
        }
        public T GetValue<T>(object obj) => (T) this.GetValue(obj);

        internal static TypePropertyInfo Create(PropertyInfo propInfo, Dictionary<Type, TypeInfo> workingTypes)
        {
            var info = new TypePropertyInfo(propInfo);
            info.ParseAttributes();
            info._propertyTypeInfo = TypeInfo.GetTypeInfo(info._property.PropertyType, workingTypes);
            return info;
        }

        public override string ToString()
        {
            return this.Name;
        }
    }
}
