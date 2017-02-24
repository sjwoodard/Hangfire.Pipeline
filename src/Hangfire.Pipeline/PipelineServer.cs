using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Pipeline
{
    using Logging;

    /// <summary>
    /// A server for executing pipeline jobs
    /// </summary>
    public class PipelineServer : IPipelineServer
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(PipelineServer));

        private readonly IPipelineStorage _pipelineStorage;
        private readonly IPipelineTaskFactory _taskFactory;

        public PipelineServer(IPipelineStorage pipelineStorage, IPipelineTaskFactory taskFactory)
        {
            if (pipelineStorage == null)
                throw new ArgumentNullException(nameof(pipelineStorage));
            if (taskFactory == null)
                throw new ArgumentNullException(nameof(taskFactory));
            _pipelineStorage = pipelineStorage;
            _taskFactory = taskFactory;
        }

        public PipelineServer(IPipelineStorage pipelineStorage)
            : this(pipelineStorage, new ReflectionPipelineTaskFactory(Assembly.GetCallingAssembly()))
        {
        }

        /// <summary>
        /// This method should be called by a Hangfire client to execute a job
        /// </summary>
        [DisplayName("{0}")]
        public void ExecuteJob(string jobContextId, IJobCancellationToken jct)
        {
            Log.InfoFormat("Begin job '{0}'", jobContextId);
            // Setup cancellation tokens
            var cts = CancellationTokenSource.CreateLinkedTokenSource(jct.ShutdownToken);
            var ct = cts.Token;
            // Read job context from storage
            Log.DebugFormat("Getting context for job '{0}'", jobContextId);
            var jobContext = GetJobContextAsync(jobContextId, ct).Result;
            if (jobContext == null)
                throw new NullReferenceException($"Missing {nameof(jobContext)}");
            jobContext.Start = DateTime.UtcNow;
            UpdateJobContextAsync(jobContext, ct).Wait();
            // Prepare the task queue
            var taskExecutions = new List<Task>();
            var queue = GetConcurrentQueue(jobContext);
            Log.DebugFormat("Job '{0}' has '{1}' tasks", jobContext.Id, jobContext.Queue.Count());
            var syncLock = new object();
            IPipelineTaskContext taskContext;
            // Begin dependency scope
            using (GetJobDependencyScope())
            {
                // Iterate over the task queue 
                while (queue.TryDequeue(out taskContext))
                {
                    if (string.IsNullOrWhiteSpace(taskContext.Task))
                        throw new InvalidOperationException("Task context does not have a task name");
                    if (string.IsNullOrWhiteSpace(taskContext.Id))
                        throw new InvalidOperationException($"Task '{taskContext.Task}' does not have an ID");
                    Log.InfoFormat("Begin task '{0}'", taskContext.Id);
                    // Check cancellation tokens
                    CheckForCancellation(jct, cts);
                    // Check if task already executed
                    if (taskContext.End > DateTime.MinValue)
                    {
                        Log.InfoFormat("Task '{0}' already executed, skipping...", taskContext.Id);
                        continue;
                    }
                    // If not parallel block thread with WaitAll until previous tasks complete
                    if (!taskContext.RunParallel)
                    {
                        Log.DebugFormat("Task '{0}' is not parallel, waiting for previous tasks to complete...",
                            taskContext.Id);
                        Task.WaitAll(taskExecutions.ToArray(), ct);
                    }
                    // Start task
                    taskContext.Start = DateTime.UtcNow;
                    OnTaskStarted(jobContext, taskContext);
                    // Execute task
                    Log.DebugFormat("Create instance of '{0}' for task '{1}'", taskContext.Task,
                        taskContext.Id);
                    var taskInstance = CreateTaskInstance(taskContext);
                    OnTaskInstanceCreated(jobContext, taskContext, taskInstance);
                    Log.DebugFormat("Execute task '{0}'", taskContext.Id);
                    var taskExecution = ExecuteTaskAsync(taskInstance, taskContext, jobContext, ct);
                    var taskContinuation = taskExecution.ContinueWith(continuation =>
                    {
                        // Get result
                        var innerTaskContext = continuation.Result;
                        // Release instance
                        Log.DebugFormat("Releasing instance of '{0}' for task '{1}'",
                            innerTaskContext.Task, innerTaskContext.Id);
                        ReleaseTaskInstance(jobContext, innerTaskContext, taskInstance);
                        // Check for task exception
                        if (continuation.Exception != null)
                            throw continuation.Exception.GetBaseException();
                        Log.InfoFormat("Finished task '{0}'", innerTaskContext.Id);
                        OnTaskExecuted(jobContext, innerTaskContext, taskInstance);
                        Log.DebugFormat("Update job '{0}' with task '{1}'",
                            jobContext.Id, innerTaskContext.Id);
                        // Update job context with results of task
                        lock (syncLock)
                        {
                            innerTaskContext.End = DateTime.UtcNow;
                            jobContext.AddCompletedTask(innerTaskContext);
                            jobContext = UpdateJobContextForTaskAsync(jobContext,
                                innerTaskContext, ct).Result;
                        }
                        OnJobContextUpdatedForTask(jobContext, innerTaskContext, taskInstance);
                    }, ct);
                    // Marshal tasks back to the worker thread
                    taskExecution.ConfigureAwait(true);
                    taskContinuation.ConfigureAwait(true);
                    taskExecutions.Add(taskExecution);
                    taskExecutions.Add(taskContinuation);
                    // If not parallel then block thread with Wait
                    if (!taskContext.RunParallel)
                    {
                        Log.DebugFormat("Task '{0}' is not parallel, waiting for completion...",
                            taskContext.Id);
                        taskExecution.Wait(cts.Token);
                    }
                }
                Task.WaitAll(taskExecutions.ToArray(), cts.Token);
            }
            // Job completion
            jobContext.End = DateTime.UtcNow;
            UpdateJobContextAsync(jobContext, ct).Wait();
            Log.InfoFormat("Finished job '{0}'", jobContext.Id);
        }

        #region Job context

        /// <summary>
        /// Converts the pipeline job queue into a concurrent queue
        /// </summary>
        protected virtual ConcurrentQueue<IPipelineTaskContext> GetConcurrentQueue(
            IPipelineJobContext jobContext)
        {
            var orderedTaskContexts = jobContext.Queue.OrderBy(task => task.Priority);
            var queue = new ConcurrentQueue<IPipelineTaskContext>();
            foreach (var taskContext in orderedTaskContexts)
                queue.Enqueue(taskContext);
            return queue;
        }

        /// <summary>
        /// Gets a job context from storage by ID
        /// </summary>
        protected virtual async Task<IPipelineJobContext> GetJobContextAsync(string jobContextId,
            CancellationToken ct)
        {
            var jobContext = await _pipelineStorage.GetJobContextAsync(jobContextId, ct);
            return jobContext;
        }

        /// <summary>
        /// Get a new scope, for use with DI and inversion of control containers
        /// </summary>
        protected virtual IPipelineTaskFactoryScope GetJobDependencyScope()
        {
            return _taskFactory.GetScope();
        }

        /// <summary>
        /// Update the job context in the pipeline storage
        /// </summary>
        protected virtual async Task<IPipelineJobContext> UpdateJobContextAsync(
            IPipelineJobContext jobContext, CancellationToken ct)
        {
            await _pipelineStorage.UpdateJobContextAsync(jobContext, ct);
            return jobContext;
        }

        /// <summary>
        /// Update a task context within a job context in the pipeline storage
        /// </summary>
        protected virtual async Task<IPipelineJobContext> UpdateJobContextForTaskAsync(
            IPipelineJobContext jobContext, IPipelineTaskContext taskContext, CancellationToken ct)
        {
            var jobTaskContext = jobContext.Queue.SingleOrDefault(task => task.Id == taskContext.Id);
            jobTaskContext = taskContext;
            await _pipelineStorage.UpdateJobContextAsync(jobContext, ct);
            return jobContext;
        }

        #endregion

        #region Task context

        /// <summary>
        /// Created a task instance from the factory
        /// </summary>
        protected virtual IPipelineTask CreateTaskInstance(IPipelineTaskContext taskContext)
        {
            var task = _taskFactory.Create(taskContext.Task);
            return task;
        }

        /// <summary>
        /// Release a task instance by calling the factory
        /// </summary>
        protected virtual void ReleaseTaskInstance(IPipelineJobContext jobContext,
            IPipelineTaskContext taskContext, IPipelineTask taskInstance)
        {
            _taskFactory.Release(taskInstance);
        }

        /// <summary>
        /// Executes a task
        /// </summary>
        protected virtual Task<IPipelineTaskContext> ExecuteTaskAsync(IPipelineTask taskInstance,
            IPipelineTaskContext taskContext, IPipelineJobContext jobContext, CancellationToken ct)
        {
            var task = taskInstance.ExecuteTaskAsync(taskContext, jobContext, _pipelineStorage, ct);
            return task;
        }

        #endregion

        #region General

        /// <summary>
        /// Check for Hangfire or internal cancellation
        /// </summary>
        /// <param name="jct">A Hangfire cancellation token</param>
        /// <param name="cts">An internal cancellation token</param>
        protected virtual void CheckForCancellation(IJobCancellationToken jct,
            CancellationTokenSource cts)
        {
            cts.Token.ThrowIfCancellationRequested();
            try
            {
                jct.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                Log.Info("Hangfire job was cancelled");
                cts.Cancel();
                throw;
            }
        }

        #endregion

        #region Hooks

        /// <summary>
        /// A hook for after the task context has been updated
        /// </summary>
        protected virtual void OnJobContextUpdatedForTask(IPipelineJobContext jobContext,
            IPipelineTaskContext taskContext, IPipelineTask taskInstance)
        {
        }

        /// <summary>
        /// A hook for after task execution has completed
        /// </summary>
        protected virtual void OnTaskExecuted(IPipelineJobContext jobContext,
            IPipelineTaskContext taskContext, IPipelineTask taskInstance)
        {
        }

        /// <summary>
        /// A hook for after a task has started
        /// </summary>
        protected virtual void OnTaskStarted(IPipelineJobContext jobContext,
            IPipelineTaskContext taskContext)
        {
        }

        /// <summary>
        /// A hook for after the task instance has been created from the factory
        /// </summary>
        protected virtual void OnTaskInstanceCreated(IPipelineJobContext jobContext,
            IPipelineTaskContext taskContext, IPipelineTask taskInstance)
        {
        }

        #endregion

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                _pipelineStorage?.Dispose();
        }
    }
}
