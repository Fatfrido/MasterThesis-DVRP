using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Optimizer.ACS
{
    public class AntColonySystemConfig
    {
        public int Iterations { get; set; }
        public int Ants { get; set; }
        public double EvaporationRate { get; set; }
        public double PheromoneImportance { get; set; }
        public int LocalSearchIterations { get; set; }
        public double PheromoneConservation { get; set; }

        public double ExploitationImportance { get; set; }
    }
}
