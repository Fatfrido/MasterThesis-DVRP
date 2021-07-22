using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVRP.Domain
{
    public class WorldState
    {
        /// <summary>
        /// Vehicle (value) that served the request (key)
        /// Used to evaluate the final cost
        /// </summary>
        public Dictionary<int, int> History { get; set; }

        /// <summary>
        /// Contains the current <see cref="Request"/> for each vehicle (index)
        /// </summary>
        public Request[] CurrentRequests { get; set; }

        /// <summary>
        /// Known requests; dynamically revealed requests are added to this dictionary
        /// </summary>
        public Dictionary<int, Request> KnownRequests { get; } = new Dictionary<int, Request>();

        /// <summary>
        /// Capacity for each vehicle (index)
        /// </summary>
        public int[] Capacities { get; set; }

        /// <summary>
        /// Total load of each vehicle - must not succeed it's capacity
        /// </summary>
        public int[] Load { get; set; }

        /// <summary>
        /// The start end endpoint of each vehicle
        /// </summary>
        public Request Depot { get; private set; }

        public long[,] CostMatrix { get; private set; }

        public int VehicleCount { get; private set; }

        /// <summary>
        /// The solution currently used for assigning requests to vehicles
        /// </summary>
        public Solution Solution { get; set; }

        public WorldState(int vehicles, Request depot, Request[] knownRequests, int[] capacities) {
            History = new Dictionary<int, int>();
            VehicleCount = vehicles;
            CurrentRequests = new Request[vehicles];
            Depot = depot;

            // Initialize the location of every vehicle with the depot
            for(int i = 0; i < vehicles; i++) {
                CurrentRequests[i] = depot;
            }

            // Add known requests
            for(int i = 0; i < knownRequests.Length; i++) {
                KnownRequests.Add(knownRequests[i].Id, knownRequests[i]);
            }

            CostMatrix = CalculateCostMatrix();
            Capacities = capacities;

            // Vehicle load at the beginning is 0
            Load = Enumerable.Repeat(0, vehicles).ToArray();
        }

        /// <summary>
        /// Add a new request
        /// </summary>
        /// <param name="request"></param>
        public void AddRequest(Request request) {
            KnownRequests.Add(request.Id, request);
            CalculateCostMatrix();
        }

        /// <summary>
        /// Commit a request to a vehicle. Cannot be undone
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="request"></param>
        public void CommitRequest(int vehicle, int request) {
            KnownRequests.Remove(request);
            History.Add(request, vehicle);
        }

        public Problem ToProblem() {
            return new Problem(
                KnownRequests.Values.ToArray(),
                VehicleCount,
                Capacities[0],
                CurrentRequests.Select(x => x.Id).ToArray(),
                CostMatrix,
                Depot.Id
                );
        }

        /// <summary>
        /// Calculates the cost matrix
        /// </summary>
        /// <returns></returns>
        private long[,] CalculateCostMatrix() {
            var requestNumber = KnownRequests.Count + 1; // mind the depot!
            var matrix = new long[requestNumber, requestNumber];

            for (int fromNode = 0; fromNode < requestNumber; fromNode++) {
                for (int toNode = 0; toNode < requestNumber; toNode++) {
                    if (fromNode == toNode) {
                        matrix[fromNode, toNode] = 1;
                    } else {
                        // Handle depot since it is not contained in KnownRequests
                        var toRequest = toNode == 0 ? Depot : KnownRequests[toNode];
                        var fromRequest = fromNode == 0 ? Depot : KnownRequests[fromNode];

                        matrix[fromNode, toNode] = (long) Math.Sqrt(Math.Pow(toRequest.X - fromRequest.X, 2) +
                                                             Math.Pow(toRequest.Y - fromRequest.Y, 2));
                    }
                }
            }

            return matrix;
        }
    }
}
