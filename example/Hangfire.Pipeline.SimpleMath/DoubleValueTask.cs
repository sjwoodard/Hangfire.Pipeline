using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Pipeline.SimpleMath
{
    public class DoubleValueTask : IPipelineTask
    {
        public Task<IPipelineTaskContext> ExecuteTaskAsync(IPipelineTaskContext taskContext, IPipelineJobContext jobContext, IPipelineStorage pipelineStorage, CancellationToken ct)
        {
            var value = taskContext.GetArg<int>("value");
            value = value * 2;
            jobContext.AddResult(taskContext.Id, value);
            return Task.FromResult(taskContext);
        }

        public void Dispose()
        {
        }
    }
}
