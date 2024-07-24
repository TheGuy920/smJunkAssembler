using System;
using System.Text.Json;
using System.Text.Json.Serialization;

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
            [JsonPropertyName("name")]
            public string Name { get; init; }

            [JsonPropertyName("description")]
            public string Description { get; init; }

            [JsonPropertyName("localId")]
            public Guid Uuid { get; init; }

            [JsonPropertyName("type")]
            public ValidModType Type { get; init; }

            [JsonPropertyName("version")]
            public int Version { get; init; }

            [JsonPropertyName("custom_icons")]
            public bool CustomIcons { get; init; }

            [JsonPropertyName("allow_add_mods")]
            public bool AllowAddMods { get; init; }

            [JsonIgnore]
            private static readonly JsonSerializerOptions jsonSerializerOptions = new()
            {
                // Converters = { new CustomEnumConverter() },
                WriteIndented = true
            };

            public string ToJson()
                => JsonSerializer.Serialize(this, jsonSerializerOptions);
        }
    }
}
