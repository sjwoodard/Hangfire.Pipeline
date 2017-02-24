using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Pipeline.SimpleMath
{
    public class SquareRootTask : IPipelineTask
    {
        public Task<IPipelineTaskContext> ExecuteTaskAsync(IPipelineTaskContext taskContext, IPipelineJobContext jobContext, IPipelineStorage pipelineStorage, CancellationToken ct)
        {
            var value = taskContext.GetArg<double>("value");
            value = Math.Sqrt(value);
            jobContext.AddResult(taskContext.Id, value);
            return Task.FromResult(taskContext);
        }

        public void Dispose()
        {
        }
    }
}
