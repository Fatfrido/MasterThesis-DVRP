using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVRP.Domain
{
    public class Problem
    {
        /// <summary>
        /// Uncommited requests
        /// </summary>
        public Request[] Requests { get; set; }

        /// <summary>
        /// Number of available vehicles
        /// </summary>
        public int VehicleCount { get; set; }

        /// <summary>
        /// Capacity of each vehicle
        /// </summary>
        public int[] VehicleCapacity { get; set; }

        /// <summary>
        /// Index of the depot (usually 0)
        /// </summary>
        public int Depot { get; set; }

        /// <summary>
        /// Starting positions of vehicles (indices)
        /// </summary>
        public int[] Start { get; set; }

        /// <summary>
        /// Cost matrix
        /// </summary>
        public long[,] CostMatrix { get; set; }

        /// <summary>
        /// Maps the index of a request to it's id
        /// </summary>
        public int[] Mapping { get; set; }

        public Problem() { }
        public Problem(Request[] requests, int vehicleCount, int[] vehicleCapacity, int[] start, long[,] costMatrix, int[] mapping, int depot = 0) {
            Requests = requests;
            VehicleCount = vehicleCount;
            VehicleCapacity = vehicleCapacity;
            Depot = depot;
            CostMatrix = costMatrix;
            Start = start;
            Mapping = mapping;
        }

        /// <summary>
        /// Returns the cost of travelling between two requests.
        /// </summary>
        /// <param name="requestId1">Id of a request</param>
        /// <param name="requestId2">Id of a request</param>
        /// <returns></returns>
        public long GetCost(int requestId1, int requestId2) {
            var i = Array.IndexOf(Mapping, requestId1);
            var j = Array.IndexOf(Mapping, requestId2);

            return CostMatrix[i, j];
        }
    }
}
