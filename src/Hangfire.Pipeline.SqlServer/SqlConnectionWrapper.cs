using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Pipeline.SqlServer
{
    public class SqlConnectionWrapper : ISqlConnectionWrapper
    {
        private readonly SqlConnection _connection;

        public SqlConnectionWrapper(SqlConnection connection)
        {
            if (connection == null)
                throw new ArgumentNullException(nameof(connection));
            _connection = connection;
        }

        public async Task OpenAsync(CancellationToken ct)
        {
            await _connection.OpenAsync(ct);
        }

        ISqlCommandWrapper ISqlConnectionWrapper.CreateCommand()
        {
            var command = _connection.CreateCommand();
            return new SqlCommandWrapper(command);
        }

        public void Close()
        {
            _connection.Close();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                _connection?.Dispose();
        }
    }
}
