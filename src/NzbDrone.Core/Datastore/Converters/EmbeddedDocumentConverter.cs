using System;
using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapper;

namespace NzbDrone.Core.Datastore.Converters
{
    public class EmbeddedDocumentConverter<T> : SqlMapper.TypeHandler<T>
    {
        private readonly JsonSerializerOptions SerializerSettings;

        public EmbeddedDocumentConverter()
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

            SerializerSettings = serializerSettings;
        }

        public EmbeddedDocumentConverter(params JsonConverter[] converters) : this()
        {
            foreach (var converter in converters)
            {
                SerializerSettings.Converters.Add(converter);
            }
        }

        public Type DbType => typeof(string);

        public override void SetValue(IDbDataParameter parameter, T doc)
        {
            parameter.Value = JsonSerializer.Serialize(doc, SerializerSettings);
        }

        public override T Parse(object value)
        {
            return JsonSerializer.Deserialize<T>((string) value, SerializerSettings);
        }
    }
}
