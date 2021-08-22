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
        public double InitialPheromoneValue { get; set; }
    }
}
