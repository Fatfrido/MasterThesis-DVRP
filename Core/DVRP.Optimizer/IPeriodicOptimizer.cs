using DVRP.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Optimizer
{
    public interface IPeriodicOptimizer
    {
        Solution Solve(Problem problem);
    }
}
