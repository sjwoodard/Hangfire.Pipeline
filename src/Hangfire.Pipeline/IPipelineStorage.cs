using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Pipeline
{
    /// <summary>
    /// Storage interface for pipeline jobs
    /// </summary>
    public interface IPipelineStorage : IDisposable
    {
        /// <summary>
        /// Creates a job context in pipeline storage -- do this before executing a job on the
        /// pipeline server
        /// </summary>
        Task<bool> CreateJobContextAsync(IPipelineJobContext jobContext, CancellationToken ct);

        /// <summary>
        /// Deletes a job context from the pipeline storage by ID
        /// </summary>
        Task<bool> DeleteJobContextAsync(string id, CancellationToken ct);

        /// <summary>
        /// Gets a job context from the pipeline storage by ID
        /// </summary>
        Task<IPipelineJobContext> GetJobContextAsync(string id, CancellationToken ct);

        /// <summary>
        /// Update a job context in the pipeline storage
        /// </summary>
        Task<bool> UpdateJobContextAsync(IPipelineJobContext jobContext, CancellationToken ct);
    }
}
