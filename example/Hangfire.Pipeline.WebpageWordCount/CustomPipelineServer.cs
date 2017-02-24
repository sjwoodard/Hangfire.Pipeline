using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Pipeline.WebpageWordCount
{
    public class CustomPipelineServer : PipelineServer
    {
        public CustomPipelineServer(IPipelineStorage pipelineStorage, IPipelineTaskFactory taskFactory)
            : base(pipelineStorage, taskFactory)
        {
        }

        [Queue("default")]
        [AutomaticRetry(Attempts = 3)]
        protected override Task<IPipelineTaskContext> ExecuteTaskAsync(IPipelineTask taskInstance, IPipelineTaskContext taskContext, IPipelineJobContext jobContext, CancellationToken ct)
        {
            return base.ExecuteTaskAsync(taskInstance, taskContext, jobContext, ct);
        }
    }
}
