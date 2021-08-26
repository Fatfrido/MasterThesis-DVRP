using DVRP.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Communication
{
    public interface IOptimizerQueue
    {
        /// <summary>
        /// Publishes a solution
        /// </summary>
        /// <param name="solution"></param>
        void Publish(Solution solution);

        /// <summary>
        /// Publishes an event that indicates the solution to start
        /// </summary>
        void PublishStart(StartSimulationMessage message);

        /// <summary>
        /// Called when a problem is received
        /// </summary>
        event EventHandler<Problem> ProblemReceived;

        /// <summary>
        /// Called when the simulation is finished and the final solution as well as its final cost is received
        /// </summary>
        event EventHandler<SimulationResult> ResultsReceived;
    }
}
