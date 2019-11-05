using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marr.Data.Converters;
using Marr.Data.Mapping;

namespace NzbDrone.Core.Datastore.Converters
{
    public class EmbeddedDocumentConverter : IConverter
    {
        private readonly JsonSerializerOptions SerializerSettings;

        public EmbeddedDocumentConverter(params JsonConverter[] converters)
        {
            var serializerSettings = new JsonSerializerOptions
            {
                AllowTrailingCommas = true,
                IgnoreNullValues = false,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true,

                // DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            };

            // serializerSettings.Converters.Add(new HttpUriConverter());
            serializerSettings.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, false));
            serializerSettings.Converters.Add(new TimeSpanConverter());

            foreach (var converter in converters)
            {
                serializerSettings.Converters.Add(converter);
            }

            SerializerSettings = serializerSettings;
        }

        public virtual object FromDB(ConverterContext context)
        {
            if (context.DbValue == DBNull.Value)
            {
                return DBNull.Value;
            }

            var stringValue = (string)context.DbValue;

            if (string.IsNullOrWhiteSpace(stringValue))
            {
                return null;
            }
            return JsonSerializer.Deserialize(stringValue, context.ColumnMap.FieldType, SerializerSettings);
        }

        public object FromDB(ColumnMap map, object dbValue)
        {
            return FromDB(new ConverterContext { ColumnMap = map, DbValue = dbValue });
        }

        public object ToDB(object clrValue)
        {
            if (clrValue == null) return null;
            if (clrValue == DBNull.Value) return DBNull.Value;

            return JsonSerializer.Serialize(clrValue, SerializerSettings);
        }

        public Type DbType => typeof(string);
    }
}
