using Newtonsoft.Json;
using System;

namespace Hangfire.Pipeline
{
    /// <summary>
    /// A JSON.Net implementation of the pipeline serializer
    /// </summary>
    public class JsonPipelineSerializer : IPipelineSerializer
    {
        private readonly Type _jobContextType;
        private readonly JsonSerializerSettings _jsonSerializerSettings;
        private readonly JsonConverter[] _converters;

        public JsonPipelineSerializer(Type jobContextType,
            JsonSerializerSettings jsonSerializerSettings)
        {
            if (jobContextType == null)
                throw new ArgumentNullException(nameof(jobContextType));
            if (jsonSerializerSettings == null)
                throw new ArgumentNullException(nameof(jsonSerializerSettings));
            _jobContextType = jobContextType;
            _jsonSerializerSettings = jsonSerializerSettings;
        }

        public JsonPipelineSerializer()
            : this(typeof(PipelineJobContext), new JsonSerializerSettings
            {
                Converters = new[] { new PipelineTaskContextJsonConverter()
            }})
        {
        }

        /// <summary>
        /// Deserializes a JSON string to a pipeline job context
        /// </summary>
        public IPipelineJobContext Deserialize(string json)
        {
            var deserialized = JsonConvert.DeserializeObject(json, _jobContextType,
                _jsonSerializerSettings);
            return (IPipelineJobContext)deserialized;
        }

        /// <summary>
        /// Serializes a pipeline job context to JSON
        /// </summary>
        public string Serialize(IPipelineJobContext jobContext)
        {
            var serialized = JsonConvert.SerializeObject(jobContext, _jsonSerializerSettings);
            return serialized;
        }
    }
}
