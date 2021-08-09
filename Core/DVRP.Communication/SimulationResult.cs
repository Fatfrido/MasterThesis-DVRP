using DVRP.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Communication
{
    public class SimulationResult : EventArgs
    {
        public Solution Solution { get; set; }
        public double Cost { get; set; }

        public SimulationResult() { }
        public SimulationResult(Solution solution, double cost) {
            Solution = solution;
            Cost = cost;
        }
    }
}
