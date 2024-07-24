using System;

namespace ModTool.Util
{
    public class ExtensionDetector
    {
        public enum FileType
        {
            Lua,
            Json,
            Image,
            Text,
            Xml,
            Unknown
        }

        public static FileType DetectFileType(string path)
        {
            string extension = System.IO.Path.GetExtension(path).ToLowerInvariant();
            if (extension.EndsWith("db", StringComparison.InvariantCultureIgnoreCase))
                return FileType.Json;
            if (extension.EndsWith("set", StringComparison.InvariantCultureIgnoreCase))
                return FileType.Json;

            return extension switch
            {
                ".lua" => FileType.Lua,
                ".json" => FileType.Json,
                ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".tiff" or ".webp" => FileType.Image,
                ".txt" => FileType.Text,
                ".xml" or ".layout" or "xaml" => FileType.Xml,
                _ => FileType.Unknown,
            };
        }
    }
}
