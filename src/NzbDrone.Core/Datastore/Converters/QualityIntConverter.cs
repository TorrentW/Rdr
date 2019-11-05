using System;
using Marr.Data.Converters;
using Marr.Data.Mapping;
using NzbDrone.Core.Qualities;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NzbDrone.Core.Datastore.Converters
{
    public class QualityIntConverter : JsonConverter<Quality>, IConverter
    {
        public object FromDB(ConverterContext context)
        {
            if (context.DbValue == DBNull.Value)
            {
                return Quality.Unknown;
            }

            var val = Convert.ToInt32(context.DbValue);

            return (Quality)val;
        }

        public object FromDB(ColumnMap map, object dbValue)
        {
            return FromDB(new ConverterContext { ColumnMap = map, DbValue = dbValue });
        }

        public object ToDB(object clrValue)
        {
            if (clrValue == DBNull.Value) return 0;

            if (clrValue as Quality == null)
            {
                throw new InvalidOperationException("Attempted to save a quality that isn't really a quality");
            }

            var quality = clrValue as Quality;
            return (int)quality;
        }

        public Type DbType => typeof(int);

        public override Quality Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions options)
        {
            var item = reader.GetInt32();
            return (Quality)item;
        }

        public override void Write(Utf8JsonWriter writer, Quality value, JsonSerializerOptions serializer)
        {
            writer.WriteNumberValue((int) value);
        }
    }
}
