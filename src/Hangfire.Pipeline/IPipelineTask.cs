using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Pipeline
{
    /// <summary>
    /// Pipeline task execution interface
    /// </summary>
    public interface IPipelineTask : IDisposable
    {
        /// <summary>
        /// Executes a task -- this class should only be called from within a job context
        /// </summary>
        /// <param name="taskContext">A task context</param>
        /// <param name="jobContext">The job context that called the task</param>
        /// <param name="pipelineStorage">The pipeline storage associated with the job</param>
        Task<IPipelineTaskContext> ExecuteTaskAsync(IPipelineTaskContext taskContext, IPipelineJobContext jobContext, IPipelineStorage pipelineStorage, CancellationToken ct);
    }
}
