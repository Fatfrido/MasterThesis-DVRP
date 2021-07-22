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
        public long[,] DistanceMatrix { get; set; }

        public Problem() { }
        public Problem(Request[] requests, int vehicleCount, int vehicleCapacity, long[,] distanceMatrix = null, int depot = 0) {
            Requests = requests;
            VehicleCount = vehicleCount;
            VehicleCapacity = vehicleCapacity;
            Depot = depot;
            DistanceMatrix = (distanceMatrix == null) ? CalcDistanceMatrix() : distanceMatrix;
        }

        public Problem(IDictionary<int, Request> requests, int vehicleCount, int vehicleCapacity, long[,] distanceMatrix = null, int depot = 0) {
            Requests = requests.Values.ToArray();
            VehicleCount = vehicleCount;
            VehicleCapacity = vehicleCapacity;
            Depot = depot;
            DistanceMatrix = (distanceMatrix == null) ? CalcDistanceMatrix() : distanceMatrix;
        }

        private long[,] CalcDistanceMatrix() {
            var requestNumber = Requests.Length;
            var matrix = new long[Requests.Length, Requests.Length];

            for(int fromNode = 0; fromNode < requestNumber; fromNode++) {
                for(int toNode = 0; toNode < requestNumber; toNode++) {
                    if(fromNode == toNode) {
                        matrix[fromNode, toNode] = 1;
                    } else {
                        matrix[fromNode, toNode] = (long) Math.Sqrt(Math.Pow(Requests[toNode].X - Requests[fromNode].X, 2) +
                                                             Math.Pow(Requests[toNode].Y - Requests[fromNode].Y, 2));
                    }
                }
            }

            return matrix;
        }
    }
}
