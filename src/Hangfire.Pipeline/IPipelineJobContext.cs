using System;
using System.Collections.Generic;

namespace Hangfire.Pipeline
{
    /// <summary>
    /// Instructions for a pipeline job to be executed on the pipeline server
    /// </summary>
    public interface IPipelineJobContext
    {
        /// <summary>
        /// A unique job ID
        /// </summary>
        string Id { get; set; }

        /// <summary>
        /// A unique Hangfire ID
        /// </summary>
        string HangfireId { get; set; }

        /// <summary>
        /// A Hangfire queue to run on, if empty jobs will run on the default queue
        /// </summary>
        string HangfireQueue { get; set; }

        /// <summary>
        /// A set of tasks that will be executed in the job context
        /// </summary>
        IEnumerable<IPipelineTaskContext> Queue { get; set; }

        /// <summary>
        /// A custom state object
        /// </summary>
        object State { get; set; }

        /// <summary>
        /// Environment variables for the job that are available to tasks at runtime
        /// </summary>
        IDictionary<string, object> Environment { get; set; }

        /// <summary>
        /// A dictionary of result objects from tasks
        /// </summary>
        IDictionary<string, object> Result { get; set; }

        /// <summary>
        /// A set of completed tasks
        /// </summary>
        IEnumerable<IPipelineTaskContext> Completed { get; set; }

        /// <summary>
        /// Timestamp when the job started
        /// </summary>
        DateTime Start { get; set; }

        /// <summary>
        /// Timestamp when the job ended
        /// </summary>
        DateTime End { get; set; }
    }
}