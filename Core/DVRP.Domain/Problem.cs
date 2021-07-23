using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVRP.Domain
{
    public class Problem
    {
        public Request[] Requests { get; set; }
        public int VehicleCount { get; set; }
        public int VehicleCapacity { get; set; }
        public int Depot { get; set; }
        public int[] Start { get; set; }
        public long[,] CostMatrix { get; set; }

        /// <summary>
        /// Maps the index of a request to it's id
        /// </summary>
        public int[] Mapping { get; set; }

        public Problem() { }
        public Problem(Request[] requests, int vehicleCount, int vehicleCapacity, int[] start, long[,] costMatrix, int[] mapping, int depot = 0) {
            Requests = requests;
            VehicleCount = vehicleCount;
            VehicleCapacity = vehicleCapacity;
            Depot = depot;
            CostMatrix = costMatrix;
            Start = start;
            Mapping = mapping;
        }
    }
}
