using System;
using System.Data.SqlClient;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Pipeline.SqlServer
{
    public interface ISqlCommandWrapper : IDisposable
    {
        string CommandText { get; set; }
        void AddParameterWithValue(string parameterName, object value);
        Task<int> ExecuteNonQueryAsync(CancellationToken ct);
        Task<object> ExecuteScalarAsync(CancellationToken ct);
    }

    public class SqlCommandWrapper : ISqlCommandWrapper
    {
        private readonly SqlCommand _command;

        public SqlCommandWrapper(SqlCommand command)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            _command = command;
        }

        public virtual string CommandText
        {
            get { return _command.CommandText; }
            set { _command.CommandText = value; }
        }

        public void AddParameterWithValue(string parameterName, object value)
        {
            _command.Parameters.AddWithValue(parameterName, value);
        }

        public async Task<int> ExecuteNonQueryAsync(CancellationToken ct)
        {
            return await _command.ExecuteNonQueryAsync(ct);
        }

        public async Task<object> ExecuteScalarAsync(CancellationToken ct)
        {
            return await _command.ExecuteScalarAsync(ct);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                _command?.Dispose();
        }
    }
}
