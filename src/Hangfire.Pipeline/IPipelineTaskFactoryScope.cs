using System;

namespace Hangfire.Pipeline
{
    /// <summary>
    /// A scope object for use with DI and inversion of control container
    /// </summary>
    public interface IPipelineTaskFactoryScope : IDisposable
    {
    }
}
