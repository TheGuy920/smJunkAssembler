using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ModTool.User.Templates
{
    public static partial class Description
    {
        public enum ValidModType
        {
            BlocksAndParts,
            CustomGame,
            TerrainAssets
        }

        internal record Default
        {
            [JsonProperty("name")]
            public string Name { get; init; }

            [JsonProperty("description")]
            public string Description { get; init; }

            [JsonProperty("localId")]
            public Guid Uuid { get; init; }

            [JsonProperty("type")]
            public ValidModType Type { get; init; }

            [JsonProperty("version")]
            public int Version { get; init; }

            [JsonProperty("custom_icons")]
            public bool CustomIcons { get; init; }

            [JsonProperty("allow_add_mods")]
            public bool AllowAddMods { get; init; }

            public string ToJson()
            {
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore,
                    Converters = new List<JsonConverter> { new CustomEnumConverter() },
                    Formatting = Formatting.Indented
                };

                return JsonConvert.SerializeObject(this, settings);
            }
        }

        // Custom JsonConverter for enum
        private partial class CustomEnumConverter : JsonConverter
        {
            public override bool CanConvert(Type objectType)
            {
                return objectType.IsEnum;
            }

            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                if (value != null)
                {
                    string enumString = UppercaseReg().Replace(value.ToString(), "$1 $2").Replace("And", "and");
                    writer.WriteValue(enumString);
                }
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                throw new NotImplementedException();
            }

            [GeneratedRegex("([a-z])([A-Z])")]
            private static partial Regex UppercaseReg();
        }
    }
}
