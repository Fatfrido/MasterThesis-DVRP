using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVRP.Domain
{
    [ProtoContract]
    public class Problem
    {
        /// <summary>
        /// Uncommited requests
        /// </summary>
        [ProtoMember(1)]
        public Request[] Requests { get; set; }

        /// <summary>
        /// Number of available vehicles
        /// </summary>
        [ProtoMember(2)]
        public int VehicleCount { get; set; }

        /// <summary>
        /// Capacity of each vehicle
        /// </summary>
        [ProtoMember(3)]
        public int[] VehicleCapacity { get; set; }

        /// <summary>
        /// Starting positions of vehicles (indices)
        /// </summary>
        [ProtoMember(4)]
        public int[] Start { get; set; }

        /// <summary>
        /// Cost matrix
        /// </summary>
        [ProtoMember(5)]
        public Matrix<long> CostMatrix { get; set; }

        /// <summary>
        /// Maps the index of a request to it's id
        /// </summary>
        [ProtoMember(6)]
        public int[] Mapping { get; set; }

        public Problem() { }
        public Problem(Request[] requests, int vehicleCount, int[] vehicleCapacity, int[] start, long[,] costMatrix, int[] mapping) {
            Requests = requests;
            VehicleCount = vehicleCount;
            VehicleCapacity = vehicleCapacity;
            CostMatrix = new Matrix<long>(costMatrix);
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
