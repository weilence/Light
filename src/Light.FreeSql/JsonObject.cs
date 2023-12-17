using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using FreeSql;
using FreeSql.Internal;
using FreeSql.Internal.Model;
using FreeSql.Internal.Model.Interface;

namespace Light.FreeSql
{
    [AttributeUsage(AttributeTargets.Property)]
    public class JsonObjectAttribute : Attribute
    {
    }

    public static class JsonObjectExtensions
    {
        public static IFreeSql UseJsonObject(this IFreeSql freeSql, JsonSerializerOptions jsonSerializerOptions = null)
        {
            jsonSerializerOptions ??= JsonSerializerOptions.Default;

            freeSql.Aop.ConfigEntityProperty += (s, e) =>
            {
                var attr = e.Property.GetCustomAttributes(typeof(JsonObjectAttribute), false).FirstOrDefault() as JsonObjectAttribute;
                if (attr == null)
                {
                    return;
                }

                switch (freeSql.Ado.DataType)
                {
                    case DataType.PostgreSQL:
                        e.ModifyResult.DbType = "jsonb";
                        e.ModifyResult.MapType = typeof(string);
                        RegisterPocoType(e.Property.PropertyType, jsonSerializerOptions);
                        break;
                    default:
                        throw new NotSupportedException($"JsonAttribute is not supported for {freeSql.Ado.DataType}");
                }
            };

            return freeSql;
        }

        private static void RegisterPocoType(Type pocoType, JsonSerializerOptions jsonSerializerOptions)
        {
            Utils.TypeHandlers.TryAdd(pocoType, new JsonTypeHandler(pocoType, jsonSerializerOptions));
        }
    }

    public class JsonTypeHandler : ITypeHandler
    {
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public JsonTypeHandler(Type type, JsonSerializerOptions jsonSerializerOptions)
        {
            _jsonSerializerOptions = jsonSerializerOptions;
            Type = type;
        }

        public object Deserialize(object value)
        {
            return JsonSerializer.Deserialize((string)value, Type, _jsonSerializerOptions);
        }

        public object Serialize(object value)
        {
            return JsonSerializer.Serialize(value, Type, _jsonSerializerOptions);
        }

        public Type Type { get; }
    }
}