using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace ModTool.ScrapMechanic.Json;

internal class JsonParser
{
    public static Dictionary<string, JsonElement> ParseJObject(string json)
    {
        return JsonDocument.Parse(json)
            .RootElement.EnumerateObject()
            .ToDictionary(jp => jp.Name, jp => jp.Value);
    }
}