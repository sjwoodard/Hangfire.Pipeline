using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace Hangfire.Pipeline.SqlServer
{
    using Logging;

    /// <summary>
    /// SQL Server implementation of Hangfire pipeline storage
    /// </summary>
    public class SqlPipelineStorage : IPipelineStorage
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(SqlPipelineStorage));
        private readonly SqlPipelineStorageOptions _options;

        public SqlPipelineStorage(SqlPipelineStorageOptions options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            _options = options;
        }

        /// <summary>
        /// Creates a job context record in SQL Server, job context ID is used as the primary key
        /// </summary>
        public async Task<bool> CreateJobContextAsync(IPipelineJobContext jobContext, CancellationToken ct)
        {
            Log.DebugFormat("Creating job context '{0}' record in SQL Server", jobContext.Id);
            var serialized = Serialize(jobContext);
            var bytes = GetBytes(serialized);
            if (_options.UseCompression)
                bytes = _options.Compression.CompressBytes(bytes);
            using (var conn = GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"insert into {_options.Table} ({_options.KeyColumn},{_options.ValueColumn}) values (@key,@value)";
                cmd.AddParameterWithValue("key", jobContext.Id);
                cmd.AddParameterWithValue("value", bytes);
                await conn.OpenAsync(ct);
                Log.Debug("Executing SQL Server INSERT command");
                var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);
                conn.Close();
                return rowsAffected > 0;
            }
        }

        public async Task<bool> DeleteJobContextAsync(string id, CancellationToken ct)
        {
            Log.DebugFormat("Getting job context '{0}' from SQL Server", id);
            using (var conn = GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"delete from {_options.Table} where {_options.KeyColumn}=@key";
                cmd.AddParameterWithValue("key", id);
                await conn.OpenAsync(ct);
                Log.Debug("Executing SQL Server DELETE command");
                var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);
                conn.Close();
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Gets a job context record by ID from SQL Server
        /// </summary>
        public async Task<IPipelineJobContext> GetJobContextAsync(string id, CancellationToken ct)
        {
            Log.DebugFormat("Getting job context '{0}' from SQL Server", id);
            byte[] bytes;
            using (var conn = GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"select top 1 {_options.ValueColumn} from {_options.Table} where {_options.KeyColumn}=@key";
                cmd.AddParameterWithValue("key", id);
                await conn.OpenAsync(ct);
                Log.Debug("Executing SQL Server SELECT command");
                var res = await cmd.ExecuteScalarAsync(ct);
                conn.Close();
                bytes = (byte[])res;
            }
            if (bytes == null)
                return null;
            if (_options.UseCompression)
                bytes = _options.Compression.DecompressBytes(bytes);
            var serialized = GetString(bytes);
            var jobContext = Deserialize(serialized);
            return jobContext;
        }

        /// <summary>
        /// Updates a job context record in SQL Server, job context ID is used as the primary key
        /// </summary>
        public async Task<bool> UpdateJobContextAsync(IPipelineJobContext jobContext, CancellationToken ct)
        {
            Log.DebugFormat("Updating job context '{0}' in SQL Server", jobContext.Id);
            var serialized = Serialize(jobContext);
            var bytes = GetBytes(serialized);
            if (_options.UseCompression)
                bytes = _options.Compression.CompressBytes(bytes);
            using (var conn = GetConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"update {_options.Table} set {_options.ValueColumn}=@value where {_options.KeyColumn}=@key";
                cmd.AddParameterWithValue("key", jobContext.Id);
                cmd.AddParameterWithValue("value", bytes);
                await conn.OpenAsync(ct);
                Log.Debug("Executing SQL Server UPDATE command");
                var rowsAffected = await cmd.ExecuteNonQueryAsync(ct);
                conn.Close();
                return rowsAffected > 0;
            }
        }

        protected virtual ISqlConnectionWrapper GetConnection()
        {
            Log.Debug("Creating SQL Server connection");
            var conn = _options.ConnectionFactory.Create();
            return conn;
        }

        protected virtual string Serialize(IPipelineJobContext jobContext)
        {
            return _options.Serializer.Serialize(jobContext);
        }

        protected virtual IPipelineJobContext Deserialize(string json) 
        {
            return _options.Serializer.Deserialize(json);
        }

        protected virtual byte[] GetBytes(string serialized)
        {
            var binary = Encoding.UTF8.GetBytes(serialized);
            return binary;
        }

        protected virtual string GetString(byte[] bytes)
        {
            var serialized = Encoding.UTF8.GetString(bytes);
            return serialized;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
