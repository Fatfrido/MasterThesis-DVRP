using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Optimizer
{
    public class ExecutionPlan
    {
        public string ProblemInstance { get; set; }
        public string Optimizer { get; set; }
        public int Iterations { get; set; }

        public override string ToString()
        {
            return $"{{ Instance: {ProblemInstance}, Optimizer: {Optimizer}, Iterations: {Iterations} }}";
        }
    }
}
