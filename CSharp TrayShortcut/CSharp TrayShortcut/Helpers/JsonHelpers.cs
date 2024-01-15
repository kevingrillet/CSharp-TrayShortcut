using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace CSharp_TrayShortcut.Helpers
{
    /// <summary>
    /// Helper class for loading and saving JSON.
    /// </summary>
    /// <typeparam name="TType">The type of object to load or save.</typeparam>
    internal static class JsonHelpers<TType> where TType : class, new()
    {
        /// <summary>
        /// Deserialise from path into TType
        /// </summary>
        /// <param name="pathConfig">Path to config file</param>
        /// <returns>Deserialized object</returns>
        /// <exception cref="ArgumentNullException">pathConfig is required</exception>
        public static TType Load(string pathConfig)
        {
            if (string.IsNullOrWhiteSpace(pathConfig))
            {
                throw new ArgumentNullException(nameof(pathConfig));
            }

            if (File.Exists(pathConfig))
            {
                return JsonConvert.DeserializeObject<TType>(File.ReadAllText(pathConfig));
            }
            else
            {
                // Handle file not found error
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

            string serializedConfig = JsonConvert.SerializeObject(config);
            if (serializedConfig != null)
            {
                File.WriteAllBytes(pathConfig, Encoding.UTF8.GetBytes(serializedConfig));
            }
        }
    }
}
