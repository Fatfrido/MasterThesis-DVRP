using DVRP.Communication;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Optimizer
{
    public class Report
    {
        public double Best { get; set; }
        public double Worst { get; set; }
        public double Average { get; set; }
        public int Invalid { get; set; }
        public int Iterations { get; set; }

        public Report(IEnumerable<SimulationResult> results)
        {
            Best = -1.0;
            Worst = -1.0;
            Invalid = 0;
            var sum = 0.0;

            foreach (var result in results)
            {
                Iterations++;

                if (!(result.Cost < 0))
                { // valid results
                    if (Best < 0 || result.Cost < Best)
                    {
                        Best = result.Cost;
                    }

                    if (Worst < 0 || result.Cost > Worst)
                    {
                        Worst = result.Cost;
                    }

                    sum += result.Cost;
                }
                else
                { // invalid solution
                    Invalid++;
                }
            }

            Average = sum / (Iterations - Invalid);
        }
    }
}
