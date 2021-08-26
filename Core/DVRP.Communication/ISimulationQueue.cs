using DVRP.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Communication
{
    public interface ISimulationQueue
    {
        /// <summary>
        /// Publishes a problem
        /// </summary>
        /// <param name="problem"></param>
        void Publish(Problem problem);

        /// <summary>
        /// Publishes the result of a simulation run
        /// </summary>
        /// <param name="result"></param>
        void Publish(SimulationResult result);

        /// <summary>
        /// Called when a optimizer requests to (re-)start the simulation
        /// </summary>
        event EventHandler<StartSimulationMessage> StartSimulationReceived;

        /// <summary>
        /// Called when a new <see cref="Solution"/> is received
        /// </summary>
        event EventHandler<Solution> SolutionReceived;
    }
}
