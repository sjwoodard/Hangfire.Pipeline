using System;

namespace Hangfire.Pipeline
{
    /// <summary>
    /// Overrides the Hangfire JobActivator to always use the pipeline server
    /// </summary>
    public class PipelineJobActivator : JobActivator
    {
        private readonly IPipelineServer _pipelineServer;

        public PipelineJobActivator(IPipelineServer pipelineServer)
        {
            _pipelineServer = pipelineServer;
        }

        public override object ActivateJob(Type jobType)
        {
            return _pipelineServer;
        }

        public override JobActivatorScope BeginScope()
        {
            return new PipelineJobActivatorScope(_pipelineServer);
        }

        private class PipelineJobActivatorScope : JobActivatorScope
        {
            private readonly IPipelineServer _pipelineServer;

            public PipelineJobActivatorScope(IPipelineServer pipelineServer)
            {
                _pipelineServer = pipelineServer;
            }

            public override object Resolve(Type type)
            {
                return _pipelineServer;
            }
        }
    }
}
