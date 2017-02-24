using System;
using System.Collections.Generic;

namespace Hangfire.Pipeline
{
    public class PipelineTaskContext : IPipelineTaskContext
    {
        public virtual string Id { get; set; }
        public virtual string Task { get; set; }
        public virtual IDictionary<string, object> Args { get; set; }
        public virtual int Priority { get; set; }
        public virtual bool RunParallel { get; set; }
        public virtual object State { get; set; }
        public virtual DateTime Start { get; set; }
        public virtual DateTime End { get; set; }
    }
}
