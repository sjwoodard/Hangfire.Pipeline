using Hangfire.Logging;
using HtmlAgilityPack;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Pipeline.WebpageWordCount.Tasks
{
    public sealed class GetWebpageTextTask : IPipelineTask
    {
        public const string Suffix = "_text";
        private static readonly ILog Log = LogProvider.GetLogger(typeof(GetWebpageTextTask));

        public GetWebpageTextTask()
        {
        }

        public Task<IPipelineTaskContext> ExecuteTaskAsync(IPipelineTaskContext taskContext, IPipelineJobContext jobContext, IPipelineStorage pipelineStorage, CancellationToken ct)
        {
            // Get URLs from the job context environment -- see TaskExtensions
            var urls = jobContext.GetUrlsFromEnvironment();

            // Strip tags for each website in the environment
            var parallelOptions = new ParallelOptions();
            parallelOptions.CancellationToken = ct;
            Parallel.ForEach(urls, url =>
            {
                Log.DebugFormat("Stripping tags from '{0}'", url);
               
                // Strip HTML tags
                var html = (string)jobContext.Result[url];
                var htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(html);
                var text = htmlDoc.DocumentNode?.SelectSingleNode("//body")?.InnerText;

                // Add result to the job context with a specific suffix
                jobContext.AddResult(url + Suffix, text);
            });
            return Task.FromResult(taskContext);
        }

        public void Dispose()
        {
        }
    }
}
