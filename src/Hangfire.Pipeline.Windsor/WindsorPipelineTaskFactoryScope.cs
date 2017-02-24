using Castle.MicroKernel.Lifestyle;
using Castle.Windsor;
using System;

namespace Hangfire.Pipeline.Windsor
{
    /// <summary>
    /// Castle Windsor implementation of Hangfire pipeline task factory scope, which starts and
    /// disposes a Windsor container scope
    /// </summary>
    public class WindsorPipelineTaskFactoryScope : IPipelineTaskFactoryScope
    {
        private IDisposable _scope;

        public WindsorPipelineTaskFactoryScope(IWindsorContainer container)
        {
            if (container == null)
                throw new ArgumentNullException(nameof(container));
            _scope = container.BeginScope();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
                _scope?.Dispose();
        }
    }
}
