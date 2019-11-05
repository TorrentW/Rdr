using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marr.Data.Converters;
using Marr.Data.Mapping;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Datastore.Converters
{
    public class TimeSpanConverter : JsonConverter<TimeSpan>, IConverter
    {
        public object FromDB(ConverterContext context)
        {
            if (context.DbValue == DBNull.Value)
            {
                return TimeSpan.Zero;
            }

            if (context.DbValue is TimeSpan)
            {
                return context.DbValue;
            }

            return TimeSpan.Parse(context.DbValue.ToString(), CultureInfo.InvariantCulture);
        }

        public object FromDB(ColumnMap map, object dbValue)
        {
            return FromDB(new ConverterContext { ColumnMap = map, DbValue = dbValue });
        }

        public object ToDB(object clrValue)
        {
            if (clrValue.ToString().IsNullOrWhiteSpace())
            {
                return null;
            }

            return ((TimeSpan)clrValue).ToString("c", CultureInfo.InvariantCulture);
        }

        public Type DbType { get; private set; }

        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return TimeSpan.Parse(reader.GetString());
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
