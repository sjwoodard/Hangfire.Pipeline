using Castle.Windsor;
using Hangfire.Logging;
using System;

namespace Hangfire.Pipeline.Windsor
{
    /// <summary>
    /// Castle Windsor implementation of the pipeline task factory, used for creating and releasing
    /// task instances
    /// </summary>
    public class WindsorPipelineTaskFactory : IPipelineTaskFactory
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(WindsorPipelineTaskFactory));

        private readonly IWindsorContainer _container;

        public WindsorPipelineTaskFactory(IWindsorContainer container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));
            _container = container;
        }

        /// <summary>
        /// Resolve a task instance by name from the Windsor container
        /// </summary>
        /// <param name="taskName">A task name registered with the container</param>
        public IPipelineTask Create(string taskName)
        {
            Log.DebugFormat("Releasing task instance '{0}'", taskName);
            var task = _container.Resolve<IPipelineTask>(taskName);
            return task;
        }

        /// <summary>
        /// Releases a task instance with the container
        /// </summary>
        public void Release(IPipelineTask taskInstance)
        {
            if (taskInstance != null)
            {
                Log.DebugFormat("Releasing task instance '{0}'", taskInstance.GetType().Name);
                _container.Release(taskInstance);
            }
        }

        /// <summary>
        /// Gets an object that can begin and dispose a Windsor scope
        /// </summary>
        public IPipelineTaskFactoryScope GetScope()
        {
            return new WindsorPipelineTaskFactoryScope(_container);
        }
    }
}
