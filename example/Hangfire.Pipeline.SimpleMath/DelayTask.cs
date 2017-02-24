using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Pipeline.SimpleMath
{
    public class DelayTask : IPipelineTask
    {
        public async Task<IPipelineTaskContext> ExecuteTaskAsync(IPipelineTaskContext taskContext, IPipelineJobContext jobContext, IPipelineStorage pipelineStorage, CancellationToken ct)
        {
            var delay = taskContext.GetArg<int>("delay");
            await Task.Delay(delay);
            return taskContext;
        }

        public void Dispose()
        {
        }
    }
}
