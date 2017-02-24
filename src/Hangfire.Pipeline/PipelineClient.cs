using System;
using System.Threading.Tasks;

namespace Hangfire.Pipeline
{
    using Logging;

    /// <summary>
    /// Default pipeline client wrapper over the Hangfire client for creating pipeline jobs
    /// </summary>
    public class PipelineClient : IPipelineClient
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(PipelineClient));

        private readonly IBackgroundJobClient _hangfireClient;

        public PipelineClient(IPipelineStorage storage, IBackgroundJobClient hangfireClient)
        {
            if (storage == null)
                throw new ArgumentNullException(nameof(storage));
            if (hangfireClient == null)
                throw new ArgumentNullException(nameof(hangfireClient));
            Storage = storage;
            _hangfireClient = hangfireClient;
        }

        public virtual IPipelineStorage Storage { get; }

        public virtual Task<IPipelineJobContext> DeleteAsync(IPipelineJobContext jobContext)
        {
            if (string.IsNullOrEmpty(jobContext.HangfireId))
                throw new ArgumentNullException(nameof(jobContext.HangfireId));
            if (_hangfireClient.Delete(jobContext.HangfireId))
            {
                Log.InfoFormat("Deleted Hangfire ID '{0}'", jobContext.HangfireId);
                jobContext.HangfireId = null;
            }
            else
            {
                Log.WarnFormat("Failed to delete Hangfire ID '{0}'", jobContext.HangfireId);
            }
            return Task.FromResult(jobContext);
        }

        public virtual Task<IPipelineJobContext> EnqueueAsync(IPipelineJobContext jobContext)
        {
            if (string.IsNullOrEmpty(jobContext.Id))
                throw new ArgumentNullException(nameof(jobContext.Id));
            jobContext.HangfireId = _hangfireClient.Enqueue<IPipelineServer>(server => 
                server.ExecuteJob(jobContext.Id, JobCancellationToken.Null));
            Log.InfoFormat("Enqueued ID '{0}' on Hangfire ID '{1}'", jobContext.Id, jobContext.HangfireId);
            return Task.FromResult(jobContext);
        }

        public virtual Task<IPipelineJobContext> RequeueAsync(IPipelineJobContext jobContext)
        {
            if (string.IsNullOrEmpty(jobContext.HangfireId))
                throw new ArgumentNullException(nameof(jobContext.HangfireId));
            if (_hangfireClient.Requeue(jobContext.HangfireId))
            {
                Log.InfoFormat("Requeued Hangfire ID '{0}'", jobContext.HangfireId);
            }
            else
            {
                Log.WarnFormat("Failed to requeue Hangfire ID '{0}'", jobContext.HangfireId);
            }
            return Task.FromResult(jobContext);
        }

        /// <summary>
        /// Schedules a pipeline job in Hangfire
        /// </summary>
        public virtual Task<IPipelineJobContext> ScheduleAsync(IPipelineJobContext jobContext,
            DateTimeOffset enqueueAt)
        {
            if (string.IsNullOrEmpty(jobContext.Id))
                throw new ArgumentNullException(nameof(jobContext.Id));
            jobContext.HangfireId = _hangfireClient.Schedule<IPipelineServer>(server =>
                server.ExecuteJob(jobContext.Id, JobCancellationToken.Null), enqueueAt);
            Log.InfoFormat("Scheduled ID '{0}' on Hangfire ID '{0}'", jobContext.Id, jobContext.HangfireId);
            return Task.FromResult(jobContext);
        }

        public virtual Task<IPipelineJobContext> ScheduleAsync(IPipelineJobContext jobContext,
            TimeSpan delay)
        {
            if (string.IsNullOrEmpty(jobContext.Id))
                throw new ArgumentNullException(nameof(jobContext.Id));
            jobContext.HangfireId = _hangfireClient.Schedule<IPipelineServer>(server =>
                server.ExecuteJob(jobContext.Id, JobCancellationToken.Null), delay);
            Log.InfoFormat("Scheduled ID '{0}' on Hangfire ID '{0}'", jobContext.Id,
                jobContext.HangfireId);
            return Task.FromResult(jobContext);
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
