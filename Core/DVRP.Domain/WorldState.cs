﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVRP.Domain
{
    public class WorldState
    {
        /// <summary>
        /// Value is a request and the key it's id
        /// Used to evaluate the final cost
        /// </summary>
        public Dictionary<int, Request> History { get; set; }

        /// <summary>
        /// Contains the current request for each vehicle (index)
        /// </summary>
        public int[] CurrentRequests { get; set; }

        /// <summary>
        /// Known requests; dynamically revealed requests are added to this dictionary
        /// Value is a request and the key it's id
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
            History = new Dictionary<int, Request>();
            VehicleCount = vehicles;
            CurrentRequests = new int[vehicles];
            Depot = depot;

            // Initialize the location of every vehicle with the depot
            for(int i = 0; i < vehicles; i++) {
                CurrentRequests[i] = depot.Id;
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
            CostMatrix = CalculateCostMatrix();
        }

        /// <summary>
        /// Commit a request to a vehicle. Cannot be undone
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="request"></param>
        public void CommitRequest(int vehicle, int request) {
            if(request != 0) { // dont mind the depot
                CurrentRequests[vehicle] = request;
                KnownRequests[request].Vehicle = vehicle;
                History.Add(KnownRequests[request].Id, KnownRequests[request]);
                KnownRequests.Remove(request);
            }
        }

        public Problem ToProblem() {
            var requests = KnownRequests.Values.ToArray();
            var mapping = new int[KnownRequests.Count + 1];

            // Create the mapping
            mapping[0] = 0; // depot
            for (int i = 1; i < mapping.Length; i++) {
                mapping[i] = requests[i - 1].Id;
            }

            var reducedCostMatrix = CreateReducedCostMatrix(mapping);

            return new Problem(
                requests,
                VehicleCount,
                Capacities[0],
                CurrentRequests,
                reducedCostMatrix,
                mapping
                );
        }

        /// <summary>
        /// Creates a cost matrix that only contains data for given requests/mapping
        /// </summary>
        /// <param name="mapping">Maps the index of a request to it's id</param>
        /// <returns></returns>
        private long[,] CreateReducedCostMatrix(int[] mapping) {
            var length = mapping.Length;
            var matrix = new long[length, length];

            // Copy only relevant parts of the cost matrix
            for(int i = 0; i < length; i++) {
                for(int j = 0; j < length; j++) {
                    matrix[i, j] = CostMatrix[mapping[i], mapping[j]];
                }
            }

            return matrix;
        }

        /// <summary>
        /// Calculates the cost matrix
        /// </summary>
        /// <returns></returns>
        private long[,] CalculateCostMatrix() { // TODO: do not recalculate whole matrix
            var requestNumber = KnownRequests.Count + History.Count + 1; // mind the depot!
            var matrix = new long[requestNumber, requestNumber];

            for (int fromNode = 0; fromNode < requestNumber; fromNode++) {
                for (int toNode = 0; toNode < requestNumber; toNode++) {
                    if (fromNode == toNode) {
                        matrix[fromNode, toNode] = 1;
                    } else {
                        var toRequest = GetRequest(toNode);
                        var fromRequest = GetRequest(fromNode);

                        matrix[fromNode, toNode] = (long) Math.Sqrt(Math.Pow(toRequest.X - fromRequest.X, 2) +
                                                             Math.Pow(toRequest.Y - fromRequest.Y, 2));
                    }
                }
            }

            return matrix;
        }

        /// <summary>
        /// Searches all known requests including already serviced ones
        /// </summary>
        /// <param name="requestId"></param>
        /// <returns></returns>
        private Request GetRequest(int requestId) {
            if(requestId == 0) { // depot
                return Depot;
            } else if(History.ContainsKey(requestId)) { // request is already served
                return History[requestId];
            } else { // request is yet to serve
                return KnownRequests[requestId];
            }
        }
    }
}
