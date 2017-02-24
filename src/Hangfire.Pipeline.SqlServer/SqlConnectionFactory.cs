using Hangfire.Logging;
using System;
using System.Data.SqlClient;

namespace Hangfire.Pipeline.SqlServer
{
    /// <summary>
    /// A factory for creating and releasing SQL connection
    /// </summary>
    public class SqlConnectionFactory : ISqlConnectionFactory
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(SqlConnectionFactory));

        private readonly string _connectionString;
        private readonly SqlCredential _credential;

        public SqlConnectionFactory(string connectionString, SqlCredential credential)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentNullException(nameof(connectionString));
            _connectionString = connectionString;
            _credential = credential;
        }

        public SqlConnectionFactory(string connectionString)
            : this(connectionString, null)
        {
        }

        public ISqlConnectionWrapper Create()
        {
            Log.Debug("Creating new SQL Server connection");
            var connection = _credential == null
                ? new SqlConnection(_connectionString)
                : new SqlConnection(_connectionString, _credential);
            return new SqlConnectionWrapper(connection);
        }

        public void Release(ISqlConnectionWrapper sqlConnection)
        {
            Log.Debug("Releasing SQL Server connection");
            sqlConnection?.Dispose();
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
