using System.Text.Json.Serialization;
using Wpf.Ui.Controls;

namespace ModTool.User.Templates
{
    public record NewFileItemTemplate
    {
        [JsonPropertyName("name")]
        public string Name { get; init; }

        [JsonPropertyName("icon")]
        public SymbolIcon Icon { get; init; }

        [JsonPropertyName("filter")]
        public string FilterTag { get; init; }

        [JsonPropertyName("description")]
        public string Description { get; init; }

        [JsonPropertyName("folder")]
        public string RelativeDirectory { get; init; }

        [JsonPropertyName("fileName")]
        public string SampleFileName { get; init; }

        [JsonPropertyName("extension")]
        public string FileExtension { get; init; }

        [JsonPropertyName("content")]
        public string FileContent { get; init; }
    }
    /*
    public class IconTypeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
            => objectType == typeof(SymbolIcon) || objectType == typeof(string);

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JToken token = JToken.Load(reader);
            if (token.Type == JTokenType.String)
            {
                string value = token.Value<string>();
                if (Enum.TryParse<SymbolRegular>(value, out var result))
                {
                    return new SymbolIcon(result);
                }
                return value; // Return the string as is if no enum match
            }
            return null;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value is SymbolIcon symbolIcon)
                writer.WriteValue(symbolIcon.Symbol.ToString());
            else
                writer.WriteValue(value.ToString());
        }
    }
    */
}
