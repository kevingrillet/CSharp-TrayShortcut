using System;
using System.IO;
using System.Text.Json;

namespace CSharp_TrayShortcut.Helpers
{
    /// <summary>
    /// Helper class for loading and saving JSON.
    /// </summary>
    /// <typeparam name="TType">The type of object to load or save.</typeparam>
    internal static class JsonHelpers<TType> where TType : class, new()
    {
        private static readonly JsonSerializerOptions _options = new()
        {
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };

        /// <summary>
        /// Deserialise from path into TType
        /// </summary>
        /// <param name="pathConfig">Path to config file</param>
        /// <returns>Deserialized object, or null if the file is missing or unreadable</returns>
        /// <exception cref="ArgumentNullException">pathConfig is required</exception>
        public static TType Load(string pathConfig)
        {
            if (string.IsNullOrWhiteSpace(pathConfig))
            {
                throw new ArgumentNullException(nameof(pathConfig));
            }

            if (!File.Exists(pathConfig))
            {
                // Handle file not found error
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<TType>(File.ReadAllText(pathConfig), _options);
            }
            catch (Exception ex) when (ex is JsonException or IOException)
            {
                // Corrupt or unreadable config: fall back to defaults.
                return null;
            }
        }

        /// <summary>
        /// Save config into file
        /// </summary>
        /// <param name="pathConfig">Path to config file</param>
        /// <param name="config">Object to save</param>
        /// <exception cref="ArgumentNullException">pathConfig &amp; config are required</exception>
        public static void Save(string pathConfig, TType config)
        {
            if (string.IsNullOrWhiteSpace(pathConfig))
            {
                throw new ArgumentNullException(nameof(pathConfig));
            }

            ArgumentNullException.ThrowIfNull(config);

            string serializedConfig = JsonSerializer.Serialize(config, _options);
            File.WriteAllText(pathConfig, serializedConfig);
        }
    }
}
