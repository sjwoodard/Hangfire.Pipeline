using System;
using System.Threading.Tasks;

namespace Hangfire.Pipeline
{
    /// <summary>
    /// A wrapper over the Hangfire client that works with pipeline jobs
    /// </summary>
    public interface IPipelineClient : IDisposable
    {
        /// <summary>
        /// Accessor for pipeline storage
        /// </summary>
        IPipelineStorage Storage { get; }

        /// <summary>
        /// Delete a job context from Hangfire so it will no longer be executed
        /// </summary>
        Task<IPipelineJobContext> DeleteAsync(IPipelineJobContext jobContext);

        /// <summary>
        /// Enqueue a job context with Hangfire to execute it as soon as possible
        /// </summary>
        Task<IPipelineJobContext> EnqueueAsync(IPipelineJobContext jobContext);

        /// <summary>
        /// Requeue a previously created Hangfire job
        /// </summary>
        Task<IPipelineJobContext> RequeueAsync(IPipelineJobContext jobContext);

        /// <summary>
        /// Schedule a job to run at some point in future
        /// </summary>
        Task<IPipelineJobContext> ScheduleAsync(IPipelineJobContext jobContext, TimeSpan delay);

        /// <summary>
        /// Schedule a job to run at some point in future
        /// </summary>
        Task<IPipelineJobContext> ScheduleAsync(IPipelineJobContext jobContext, DateTimeOffset delay);
    }
}
