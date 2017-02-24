using Hangfire.Logging;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Linq;

namespace Hangfire.Pipeline.WebpageWordCount.Tasks
{
    public class LogResultTask : IPipelineTask
    {
        private static readonly ILog Log = LogProvider.GetLogger(typeof(LogResultTask));

        public LogResultTask()
        {
        }

        public Task<IPipelineTaskContext> ExecuteTaskAsync(IPipelineTaskContext taskContext, IPipelineJobContext jobContext, IPipelineStorage pipelineStorage, CancellationToken ct)
        {
            // Get the count words results
            var countWordTasks = jobContext.Result.Where(result => result.Key.Contains(CountWordsTask.Suffix));
            var serialized = JsonConvert.SerializeObject(countWordTasks);
            Log.Info(serialized);
            return Task.FromResult(taskContext);
        }

        public void Dispose()
        {
        }
    }
}
