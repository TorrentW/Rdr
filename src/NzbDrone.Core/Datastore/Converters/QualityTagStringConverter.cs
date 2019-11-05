using System;
using Marr.Data.Converters;
using Marr.Data.Mapping;
using NzbDrone.Core.CustomFormats;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace NzbDrone.Core.Datastore.Converters
{
    public class QualityTagStringConverter : JsonConverter<FormatTag>, IConverter
    {
        public object FromDB(ConverterContext context)
        {
            if (context.DbValue == DBNull.Value)
            {
                return new FormatTag(""); //Will throw argument exception!
            }

            var val = Convert.ToString(context.DbValue);

            return new FormatTag(val);
        }

        public object FromDB(ColumnMap map, object dbValue)
        {
            return FromDB(new ConverterContext { ColumnMap = map, DbValue = dbValue });
        }

        public object ToDB(object clrValue)
        {
            if(clrValue == DBNull.Value) return 0;

            if(!(clrValue is FormatTag))
            {
                throw new InvalidOperationException("Attempted to save a quality tag that isn't really a quality tag");
            }

            var quality = (FormatTag) clrValue;
            return quality.Raw;
        }

        public Type DbType => typeof(string);

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(FormatTag);
        }

        public override FormatTag Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions options)
        {
            var item = reader.GetString();
            return new FormatTag(Convert.ToString(item));
        }

        public override void Write(Utf8JsonWriter writer, FormatTag value, JsonSerializerOptions options)
        {
            writer.WriteStringValue((string)ToDB(value));
        }
    }
}
