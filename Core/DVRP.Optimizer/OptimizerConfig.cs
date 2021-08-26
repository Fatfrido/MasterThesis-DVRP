using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Optimizer
{
    public class OptimizerConfig
    {
        public string PublishConnectionString { get; set; }
        public string SubscribeConnectionString { get; set; }
        public string ExecutionPlan { get; set; }
    }
}
