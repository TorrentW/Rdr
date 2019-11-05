using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marr.Data.Converters;
using Marr.Data.Mapping;
using NzbDrone.Core.Languages;

namespace NzbDrone.Core.Datastore.Converters
{
    public class LanguageIntConverter : JsonConverter<Language>, IConverter
    {
        public object FromDB(ConverterContext context)
        {
            if (context.DbValue == DBNull.Value)
            {
                return Language.Unknown;
            }

            var val = Convert.ToInt32(context.DbValue);

            return (Language)val;
        }

        public object FromDB(ColumnMap map, object dbValue)
        {
            return FromDB(new ConverterContext { ColumnMap = map, DbValue = dbValue });
        }

        public object ToDB(object clrValue)
        {
            if (clrValue == DBNull.Value) return 0;

            if (clrValue as Language == null)
            {
                throw new InvalidOperationException("Attempted to save a language that isn't really a language");
            }

            var language = clrValue as Language;
            return (int)language;
        }

        public Type DbType
        {
            get
            {
                return typeof(int);
            }
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(Language);
        }

        public override Language Read(ref Utf8JsonReader reader, Type objectType, JsonSerializerOptions options)
        {
            var item = reader.GetInt32();
            return (Language)item;
        }

        public override void Write(Utf8JsonWriter writer, Language value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue((int) value);
        }
    }
}
