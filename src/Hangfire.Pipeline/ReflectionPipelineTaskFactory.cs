using System;
using System.Reflection;

namespace Hangfire.Pipeline
{
    using Logging;

    public class ReflectionPipelineTaskFactory : IPipelineTaskFactory
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(ReflectionPipelineTaskFactory));

        private readonly Assembly _assembly;

        public ReflectionPipelineTaskFactory(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));
            _assembly = assembly;
        }

        /// <summary>
        /// Creates a new task via reflection
        /// </summary>
        /// <param name="taskName">A full type name</param>
        public virtual IPipelineTask Create(string taskName)
        {
            Log.DebugFormat("Creating instance of '{0}' using reflection", taskName);
            var typ = _assembly.GetType(taskName);
            if (typ == null)
                throw new NullReferenceException($"Task '{taskName}' could not be resolved via reflection using assembly '{_assembly.FullName}', check your assembly and task name");
            var task = (IPipelineTask)Activator.CreateInstance(typ);
            return task;
        }

        /// <summary>
        /// Releases a task instance
        /// </summary>
        public virtual void Release(IPipelineTask taskInstance)
        {
            if (taskInstance != null)
            {
                Log.DebugFormat("Releasing instance of '{0}'", taskInstance.GetType().FullName);
                taskInstance.Dispose();
            }
        }

        /// <summary>
        /// This task factory does not use scoping
        /// </summary>
        public IPipelineTaskFactoryScope GetScope()
        {
            return null;
        }
    }
}
