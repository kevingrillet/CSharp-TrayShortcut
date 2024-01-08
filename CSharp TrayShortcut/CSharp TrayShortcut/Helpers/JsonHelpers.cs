using Newtonsoft.Json;

namespace CSharp_TrayShortcut.Helpers
{
    /// <summary>
    /// Help Load/Save Json.
    /// </summary>
    /// <typeparam name="TType"></typeparam>
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
            return JsonConvert.DeserializeObject<TType>(File.ReadAllText(pathConfig));
        }

        /// <summary>
        /// Save config into file
        /// </summary>
        /// <param name="pathConfig">Path to config file</param>
        /// <param name="config">Object to save</param>
        /// <exception cref="ArgumentNullException">pathConfig & config are required</exception>
        public static void Save(string pathConfig, TType config)
        {
            if (string.IsNullOrWhiteSpace(pathConfig))
            {
                throw new ArgumentNullException(nameof(pathConfig));
            }
            ArgumentNullException.ThrowIfNull(config);
            File.WriteAllText(pathConfig, JsonConvert.SerializeObject(config));
        }
    }
}
