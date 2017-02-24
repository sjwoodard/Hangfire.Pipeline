namespace Hangfire.Pipeline.SqlServer
{
    public class SqlPipelineStorageOptions
    {
        /// <summary>
        /// A connection factory for creating and releasing SQL Connections
        /// </summary>
        public ISqlConnectionFactory ConnectionFactory { get; set; }

        /// <summary>
        /// A serializer for converting job contexts into flat text for storage
        /// </summary>
        public IPipelineSerializer Serializer { get; set; } = new JsonPipelineSerializer();

        /// <summary>
        /// A utility for (de)compressing SQL Server values, defualt uses GZip compression.
        /// To disable this, set UseCompression to false
        /// </summary>
        public ISqlPipelineStorageCompression Compression { get; set; } = new GzipCompression();

        /// <summary>
        /// The table in which to store data
        /// </summary>
        public string Table { get; set; }

        /// <summary>
        /// The primary key column name
        /// </summary>
        public string KeyColumn { get; set; }

        /// <summary>
        /// The column name in which data values will be stored
        /// </summary>
        public string ValueColumn { get; set; }

        /// <summary>
        /// Compresses the value column if true (default)
        /// </summary>
        public bool UseCompression { get; set; } = true;
    }
}
