using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Harvest.Api
{
    class TimeSpanConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TimeSpan) || objectType == typeof(TimeSpan?);
        }

        public override bool CanWrite => false;

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var baseType = Nullable.GetUnderlyingType(objectType);

            if (baseType != null && reader.TokenType == JsonToken.Null)
                return null;

            if (reader.TokenType == JsonToken.String && DateTime.TryParse((string)reader.Value, out DateTime date))
                return date.TimeOfDay;

            throw new JsonSerializationException($"Unexpected token or value when parsing version. Token: {reader.TokenType}, Value: {reader.Value}");
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
