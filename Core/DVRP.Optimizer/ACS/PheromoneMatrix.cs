using DVRP.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Optimizer.ACS
{
    public class PheromoneMatrix
    {
        private double initialPheromoneValue;
        private double[,] pheromones;

        public PheromoneMatrix(Problem problem, double initialPheromoneValue) {
            this.initialPheromoneValue = initialPheromoneValue;
        }
    }
}
