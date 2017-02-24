namespace Hangfire.Pipeline
{
    /// <summary>
    /// Handles creation and disposal of task instances
    /// </summary>
    public interface IPipelineTaskFactory
    {
        /// <summary>
        /// Create a new instance of a task
        /// </summary>
        /// <param name="taskName">A task name</param>
        IPipelineTask Create(string taskName);

        /// <summary>
        /// Release (dispose) a task instance
        /// </summary>
        void Release(IPipelineTask taskInstance);

        /// <summary>
        /// Gets a new scope from a DI or inversion of control container -- if available this
        /// allows you to reuse task instances within a single job context and disposes them
        /// after the job is complete
        /// </summary>
        IPipelineTaskFactoryScope GetScope();
    }
}
