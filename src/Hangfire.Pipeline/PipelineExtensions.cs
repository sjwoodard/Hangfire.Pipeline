using System;
using System.Collections.Generic;
using System.Linq;

namespace Hangfire.Pipeline
{
    using Logging;

    public static class PipelineExtensions
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(PipelineExtensions));

        #region Job context

        /// <summary>
        /// Add a task to a job context queue
        /// </summary>
        public static void QueueTask(this IPipelineJobContext jobContext, IPipelineTaskContext taskContext)
        {
            if (jobContext.Queue == null)
                jobContext.Queue = new PipelineTaskContext[] { };
            jobContext.Queue = jobContext.Queue.Concat(new[] { taskContext });
        }

        /// <summary>
        /// Add an environment variable to a job context
        /// </summary>
        public static void AddEnvironment(this IPipelineJobContext jobContext, string key, object value)
        {
            if (jobContext.Environment == null)
                jobContext.Environment = new Dictionary<string, object>();
            jobContext.Environment.Add(key, value);
        }

        /// <summary>
        /// Get a task argument
        /// </summary>
        /// <typeparam name="T">Argument value type</typeparam>
        public static T GetEnvironment<T>(this IPipelineJobContext jobContext, string key)
        {
            if (jobContext.Environment == null)
                throw new NullReferenceException("Job context does not have any environment variables");
            if (!jobContext.Environment.ContainsKey(key))
                throw new KeyNotFoundException($"Job context does not contain an environment variable named '{key}'");
            var env = (T)Convert.ChangeType(jobContext.Environment[key], typeof(T));
            return env;
        }

        /// <summary>
        /// Add a result object to a job context
        /// </summary>
        public static void AddResult(this IPipelineJobContext jobContext, string key, object value)
        {
            if (jobContext.Result == null)
                jobContext.Result = new Dictionary<string, object>();
            jobContext.Result.Add(key, value);
        }

        /// <summary>
        /// Get a result object by key from the job context
        /// </summary>
        /// <typeparam name="T">Result value type</typeparam>
        public static T GetResult<T>(this IPipelineJobContext jobContext, string key)
        {
            if (jobContext.Result == null)
                throw new NullReferenceException("Job context does not have any results");
            if (!jobContext.Result.ContainsKey(key))
                throw new KeyNotFoundException($"Job context does not contain a result named '{key}'");
            var result = (T)Convert.ChangeType(jobContext.Result[key], typeof(T));
            return result;
        }

        /// <summary>
        /// Add a result object to a job context
        /// </summary>
        public static void AddCompletedTask(this IPipelineJobContext jobContext, IPipelineTaskContext taskContext)
        {
            if (jobContext.Completed == null)
                jobContext.Completed = new PipelineTaskContext[] { };
            jobContext.Completed = jobContext.Completed.Concat(new[] { taskContext });
        }

        #endregion

        #region Task context

        /// <summary>
        /// Set a task argument
        /// </summary>
        public static void AddArg(this IPipelineTaskContext taskContext, string key, object value)
        {
            if (taskContext.Args == null)
                taskContext.Args = new Dictionary<string, object>();
            taskContext.Args.Add(key, value);
        }

        /// <summary>
        /// Get a task argument
        /// </summary>
        /// <typeparam name="T">Argument value type</typeparam>
        public static T GetArg<T>(this IPipelineTaskContext taskContext, string key)
        {
            if (taskContext.Args == null)
                throw new NullReferenceException("Job context does not have any arguments");
            if (!taskContext.Args.ContainsKey(key))
                throw new KeyNotFoundException($"Task context does not contain an argument named '{key}'");
            var arg = (T)Convert.ChangeType(taskContext.Args[key], typeof(T));
            return arg;
        }

        #endregion

        #region General

        /// <summary>
        /// Convert a dictionary to a generic object
        /// </summary>
        /// <typeparam name="T">Object result type</typeparam>
        public static T ToObject<T>(this IDictionary<string, object> source) where T : class, new()
        {
            var obj = new T();
            var typ = typeof(T);
            foreach (KeyValuePair<string, object> item in source)
            {
                try
                {
                    typ.GetProperty(item.Key).SetValue(obj, item.Value, null);
                }
                catch (Exception ex)
                {
                    Log.WarnException(ex.Message, ex);
                    continue;
                }
            }
            return obj;
        }

        #endregion
    }
}
 