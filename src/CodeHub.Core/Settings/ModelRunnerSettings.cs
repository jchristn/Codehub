namespace CodeHub.Core.Settings
{
    /// <summary>
    /// Model-runner settings (JSON section "modelRunner"). Reserved for a future feature that
    /// uses an LLM endpoint; not consumed yet.
    /// </summary>
    public class ModelRunnerSettings
    {
        #region Public-Members

        /// <summary>
        /// Endpoint base URL of the model runner.
        /// </summary>
        public string EndpointBaseUrl { get; set; } = "";

        /// <summary>
        /// API type: OpenAI, Gemini, or Ollama.
        /// </summary>
        public string ApiType { get; set; } = "OpenAI";

        /// <summary>
        /// Authentication material (API key / token) for the endpoint.
        /// </summary>
        public string ApiKey { get; set; } = "";

        /// <summary>
        /// Model name to use.
        /// </summary>
        public string ModelName { get; set; } = "";

        #endregion
    }
}
