using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Optimizer.GA
{
    public class GeneticAlgorithmConfig
    {
        public int PopulationSize { get; set; }
        public int KTournament { get; set; }
        public double MutationRate { get; set; }
        public int Elites { get; set; }
        public int InitialIterations { get; set; }
        public double RandomInsertionRate { get; set; }
    }
}
