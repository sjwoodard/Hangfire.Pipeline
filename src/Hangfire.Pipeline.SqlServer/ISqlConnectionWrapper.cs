using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Pipeline.SqlServer
{
    public interface ISqlConnectionWrapper : IDisposable
    {
        void Close();
        Task OpenAsync(CancellationToken ct);
        ISqlCommandWrapper CreateCommand();
    }
}