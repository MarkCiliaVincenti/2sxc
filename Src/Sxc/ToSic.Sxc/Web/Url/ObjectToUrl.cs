﻿using System;
using System.Collections;
using System.Linq;
using ToSic.Eav.Plumbing;

namespace ToSic.Sxc.Web.Url
{
    public class ObjectToUrl
    {
        public ObjectToUrl(string prefix = null)
        {
            Prefix = prefix;
        }
        public string Prefix { get; }
        public string ArraySeparator { get; set; }

        public string PairSeparator { get; set; } = UrlParts.ValuePairSeparator.ToString();

        public string KeyValueSeparator { get; set; } = "=";


        public string Obj2Url(object data)
        {
            return Serialize(data, Prefix);
        }

        private ValuePair ValueSerialize(object value, string propName, Type valueType)
        {
            if (value == null) return new ValuePair(propName, null);
            if (value is string strValue) return new ValuePair(propName, strValue);

            // Check array - not sure yet if we care
            if (value is IEnumerable enumerable)
            {
                var valueElemType = valueType.IsGenericType
                    ? valueType.GetGenericArguments()[0]
                    : valueType.GetElementType();
                if (valueElemType != null && valueElemType.IsPrimitive || valueElemType == typeof(string))
                {
                    // var enumerable = properties[key] as IEnumerable;
                    return new ValuePair(propName, string.Join(ArraySeparator, enumerable.Cast<object>()));
                }

                return new ValuePair(propName, "array-lik-but-unclear");
            }

            return value.GetType().IsSimpleType() 
                ? new ValuePair(propName, value.ToString()) 
                : new ValuePair(null, Serialize(value, propName + ":"), true);
        }

        // https://ole.michelsen.dk/blog/serialize-object-into-a-query-string-with-reflection/
        // https://stackoverflow.com/questions/6848296/how-do-i-serialize-an-object-into-query-string-format
        public string Serialize(object objToConvert, string prefix)
        {
            if (objToConvert == null)
                throw new ArgumentNullException(nameof(objToConvert));

            // Get all properties on the object
            var properties = objToConvert.GetType().GetProperties()
                .Where(x => x.CanRead)
                .Select(x => ValueSerialize(x.GetValue(objToConvert, null), prefix + x.Name, x.GetType()))
                .Where(x => x.Value != null)
                .ToList();

            // Concat all key/value pairs into a string separated by ampersand
            return string.Join(PairSeparator, properties.Select(p => p.ToString()));

        }

        private class ValuePair
        {

            public ValuePair(string name, string value, bool isEncoded = false)
            {
                Name = name;
                Value = value;
                IsEncoded = isEncoded;
            }
            public string Name { get; }
            public string Value { get; }
            public bool IsEncoded { get; }

            public override string ToString()
            {
                var start = Name != null ? Name + "=" : null;
                var val = IsEncoded ? Value : Uri.EscapeDataString(Value);
                return $"{start}{val}";
            }
        }
    }
}