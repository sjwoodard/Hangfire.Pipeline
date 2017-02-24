using Hangfire.Logging;
using System;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Hangfire.Pipeline.WebpageWordCount.Tasks
{
    public sealed class CountWordsTask : IPipelineTask
    {
        public const string Suffix = "_count";
        private static readonly ILog Log = LogProvider.GetLogger(typeof(CountWordsTask));

        public CountWordsTask()
        {
        }

        public Task<IPipelineTaskContext> ExecuteTaskAsync(IPipelineTaskContext taskContext, IPipelineJobContext jobContext, IPipelineStorage pipelineStorage, CancellationToken ct)
        {
            // Get URLs from the job context environment -- see TaskExtensions
            var urls = jobContext.GetUrlsFromEnvironment();

            // Get Regex pattern
            var patternArg = taskContext.GetArg<string>("pattern");
            if (string.IsNullOrEmpty(patternArg))
                throw new ArgumentNullException("pattern");
            var pattern = new Regex(patternArg);

            // Iterate over all the stripped tags
            var parallelOptions = new ParallelOptions();
            parallelOptions.CancellationToken = ct;
            Parallel.ForEach(urls, url =>
            {
                Log.DebugFormat("Counting words from '{0}'", url);

                // Get the text from the job context results
                var text = jobContext.GetResult<string>(url + GetWebpageTextTask.Suffix);
                if (string.IsNullOrEmpty(text))
                    return;

                // Tokenize the text
                var tokens = pattern.Matches(text);

                // Add the result to the job context using a specific suffix
                jobContext.AddResult(url + Suffix, tokens.Count);
            });
            return Task.FromResult(taskContext);
        }

        public void Dispose()
        {
        }
    }
}
