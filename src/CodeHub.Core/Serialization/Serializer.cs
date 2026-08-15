namespace CodeHub.Core.Serialization
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization;

    /// <summary>
    /// JSON serialization helper. Emits camelCase property names so the React
    /// dashboard consumes a consistent contract, and renders enums as strings.
    /// </summary>
    public class Serializer
    {
        #region Private-Members

        private readonly JsonSerializerOptions _Indented;
        private readonly JsonSerializerOptions _Compact;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the serializer.
        /// </summary>
        public Serializer()
        {
            _Indented = BuildOptions(true);
            _Compact = BuildOptions(false);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Serialize an object to JSON.
        /// </summary>
        /// <param name="obj">Object to serialize.</param>
        /// <param name="pretty">Whether to indent the output.</param>
        /// <returns>JSON string.</returns>
        public string SerializeJson(object obj, bool pretty = false)
        {
            if (obj == null) return null;
            return JsonSerializer.Serialize(obj, pretty ? _Indented : _Compact);
        }

        /// <summary>
        /// Deserialize a JSON string to a typed object.
        /// </summary>
        /// <typeparam name="T">Target type.</typeparam>
        /// <param name="json">JSON string.</param>
        /// <returns>Deserialized object, or default if the string is empty.</returns>
        public T DeserializeJson<T>(string json)
        {
            if (String.IsNullOrEmpty(json)) return default;
            return JsonSerializer.Deserialize<T>(json, _Compact);
        }

        #endregion

        #region Private-Methods

        private static JsonSerializerOptions BuildOptions(bool indented)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                WriteIndented = indented,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }

        #endregion
    }
}
