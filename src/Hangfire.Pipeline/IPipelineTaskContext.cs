using System;
using System.Collections.Generic;

namespace Hangfire.Pipeline
{
    /// <summary>
    /// Instructions for a task execution
    /// </summary>
    public interface IPipelineTaskContext
    {
        /// <summary>
        /// A unique task ID
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// A task name used when creating tasks from the factory
        /// </summary>
        string Task { get; set; }

        /// <summary>
        /// A dictionary of arguments that are used in task at runtime
        /// </summary>
        IDictionary<string, object> Args { get; set; }

        /// <summary>
        /// Determines the order tasks will be executed
        /// </summary>
        int Priority { get; set; }

        /// <summary>
        /// When true, the task will run in parallel with other tasks. When false, the task will be blocking,
        /// meaning that it will wait on all previous tasks to complete before execution and subsequent tasks
        /// will be executed once the task is complete.
        /// </summary>
        bool RunParallel { get; set; }

        /// <summary>
        /// A custom state object
        /// </summary>
        object State { get; set; }

        /// <summary>
        /// Timstamp when the task started
        /// </summary>
        DateTime Start { get; set; }

        /// <summary>
        /// Timestamp when the task ended
        /// </summary>
        DateTime End { get; set; }
    }
}
