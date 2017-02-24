using System;
using System.Collections.Generic;

namespace Hangfire.Pipeline.WebpageWordCount.Tasks
{
    public static class TaskExtensions
    {
        /// <summary>
        /// Convert a comma separated list of URLs from job context environment to an enumerable
        /// </summary>
        public static IEnumerable<string> GetUrlsFromEnvironment(this IPipelineJobContext jobContext)
        {
            var environmentUrls = jobContext.GetEnvironment<string>("urls");
            if (string.IsNullOrEmpty(environmentUrls))
                throw new ArgumentNullException("urls");
            var urls = environmentUrls.Split(',');
            return urls;
        }
    }
}
