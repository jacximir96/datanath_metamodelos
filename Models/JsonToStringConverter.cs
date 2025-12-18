using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DataNath.ApiMetadatos.Models;

public class JsonToStringConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(string);
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return string.Empty;
        }

        if (reader.TokenType == JsonToken.String)
        {
            return reader.Value?.ToString() ?? string.Empty;
        }

        // Si es un array o un objeto, lo convertimos a string JSON
        var token = JToken.Load(reader);
        return token.ToString(Formatting.None);
    }

    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value == null || string.IsNullOrEmpty(value.ToString()))
        {
            writer.WriteNull();
            return;
        }

        var stringValue = value.ToString();

        // Intentar parsear como JSON para escribirlo correctamente
        try
        {
            var token = JToken.Parse(stringValue!);
            token.WriteTo(writer);
        }
        catch
        {
            // Si no es JSON válido, escribirlo como string
            writer.WriteValue(stringValue);
        }
    }
}
