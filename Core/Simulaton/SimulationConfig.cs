using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Simulaton
{
    public class SimulationConfig
    {
        public string PublishConnectionString { get; set; }
        public string SubscribeConnectionString { get; set; }
        public string ProblemInstanceName { get; set; }
    }
}
