using System;
using System.IO;
using System.Text;
using System.Text.Json;
namespace ModTool.Utilities
{
    /// <summary>Provides methods to load and save the application configuration. </summary>
    public static class JsonApplicationConfiguration
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new() { WriteIndented = true };
        private const string ConfigExtension = ".json";
        private const string SchemaExtension = ".schema.json";

        /// <summary>Loads the application configuration. </summary>
        /// <typeparam name="T">The type of the application configuration. </typeparam>
        /// <param name="fileNameWithoutExtension">The configuration file name without extension. </param>
        /// <param name="alwaysCreateNewSchemaFile">Defines if the schema file should always be generated and overwritten. </param>
        /// <param name="storeInAppData">Defines if the configuration file should be loaded from the user's AppData directory. </param>
        /// <returns>The configuration object. </returns>
        /// <exception cref="IOException">An I/O error occurred while opening the file. </exception>
        public static T Load<T>(string fileNameWithoutExtension, bool alwaysCreateNewSchemaFile, bool storeInAppData) where T : new()
        {
            var configPath = CreateFilePath(fileNameWithoutExtension, ConfigExtension, storeInAppData);
            var schemaPath = CreateFilePath(fileNameWithoutExtension, SchemaExtension, storeInAppData);

            if (alwaysCreateNewSchemaFile || !File.Exists(schemaPath))
                CreateSchemaFile<T>(fileNameWithoutExtension, storeInAppData);

            if (!File.Exists(configPath))
                return CreateDefaultConfigurationFile<T>(fileNameWithoutExtension, storeInAppData);

            return JsonSerializer.Deserialize<T>(File.ReadAllText(configPath, Encoding.UTF8));
        }

        /// <summary>Saves the configuration. </summary>
        /// <param name="fileNameWithoutExtension">The configuration file name without extension. </param>
        /// <param name="configuration">The configuration object to store. </param>
        /// <param name="storeInAppData">Defines if the configuration file should be stored in the user's AppData directory. </param>
        /// <exception cref="IOException">An I/O error occurred while opening the file. </exception>
        public static void Save<T>(string fileNameWithoutExtension, T configuration, bool storeInAppData) where T : new()
        {
            CreateSchemaFile<T>(fileNameWithoutExtension, storeInAppData);

            var configPath = CreateFilePath(fileNameWithoutExtension, ConfigExtension, storeInAppData);
            File.WriteAllText(configPath, JsonSerializer.Serialize(configuration, JsonSerializerOptions), Encoding.Unicode);
        }

        private static string CreateFilePath(string fileNameWithoutExtension, string extension, bool storeInAppData)
        {
            if (storeInAppData)
            {
                var appDataDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var filePath = Path.Combine(appDataDirectory, fileNameWithoutExtension) + extension;

                var directoryPath = Path.GetDirectoryName(filePath);
                if (directoryPath != null && !Directory.Exists(directoryPath))
                    Directory.CreateDirectory(directoryPath);

                return filePath;
            }
            return fileNameWithoutExtension + extension;
        }

        private static T CreateDefaultConfigurationFile<T>(string fileNameWithoutExtension, bool storeInAppData) where T : new()
        {
            var config = new T();
            var configData = JsonSerializer.Serialize(config, JsonSerializerOptions);
            var configPath = CreateFilePath(fileNameWithoutExtension, ConfigExtension, storeInAppData);

            File.WriteAllText(configPath, configData, Encoding.Unicode);
            return config;
        }

        private static void CreateSchemaFile<T>(string fileNameWithoutExtension, bool storeInAppData) where T : new()
        {
            /*
            var schemaPath = CreateFilePath(fileNameWithoutExtension, SchemaExtension, storeInAppData);
            var schema = Task.Run(() => JsonSchema.FromType<T>()).GetAwaiter().GetResult();

            File.WriteAllText(schemaPath, schema.ToJson(), Encoding.UTF8);*/
        }
    }
}