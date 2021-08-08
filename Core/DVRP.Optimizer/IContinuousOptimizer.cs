using DVRP.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Optimizer
{
    public interface IContinuousOptimizer
    {
        /// <summary>
        /// Handles changes made to the world state
        /// </summary>
        /// <param name="problem"></param>
        void HandleNewProblem(Problem problem);

        event EventHandler<Solution> NewBestSolutionFound;
    }
}
