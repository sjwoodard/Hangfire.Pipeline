namespace Hangfire.Pipeline
{
    /// <summary>
    /// A serializer for pipeline jobs
    /// </summary>
    public interface IPipelineSerializer
    {
        /// <summary>
        /// Deserialize s JSON string to a job context
        /// </summary>
        IPipelineJobContext Deserialize(string serialized);

        /// <summary>
        /// Serialize a job context to a JSON string
        /// </summary>
        string Serialize(IPipelineJobContext jobContext);
    }
}