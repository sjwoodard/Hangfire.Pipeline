using System;

namespace Hangfire.Pipeline
{
    /// <summary>
    /// A server for executing pipeline jobs
    /// </summary>
    public interface IPipelineServer : IDisposable
    {
        /// <summary>
        /// This method should be called by a Hangfire client to execute a job
        /// </summary>
        /// <param name="jobContextId">A job context ID that is available in pipeline storage</param>
        /// <param name="jct">A Hangfire cancellation token, use JobCancellationToken.Null</param>
        void ExecuteJob(string jobContextId, IJobCancellationToken jct);
    }
}
