using System;

namespace Hangfire.Pipeline.SqlServer
{
    /// <summary>
    /// A factory for creating and releasing SQL connection
    /// </summary>
    public interface ISqlConnectionFactory : IDisposable
    {
        ISqlConnectionWrapper Create();
        void Release(ISqlConnectionWrapper sqlConnection);
    }
}