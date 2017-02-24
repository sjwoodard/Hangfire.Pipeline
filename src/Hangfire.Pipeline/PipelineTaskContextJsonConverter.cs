using Newtonsoft.Json.Converters;
using System;

namespace Hangfire.Pipeline
{
    public class PipelineTaskContextJsonConverter : CustomCreationConverter<IPipelineTaskContext>
    {
        public override IPipelineTaskContext Create(Type objectType)
        {
            return new PipelineTaskContext();
        }
    }
}
