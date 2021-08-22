using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace DVRP.Domain
{
    public class WorldState
    {
        /// <summary>
        /// Value is a request and the key it's id
        /// Used to evaluate the final cost
        /// </summary>
        public Dictionary<int, Request> History { get; private set; }

        /// <summary>
        /// Contains the current request for each vehicle (index)
        /// </summary>
        public int[] CurrentRequests { get; private set; }

        /// <summary>
        /// Known requests; dynamically revealed requests are added to this dictionary
        /// Value is a request and the key it's id
        /// </summary>
        public Dictionary<int, Request> KnownRequests { get; } = new Dictionary<int, Request>();

        /// <summary>
        /// Capacity for each vehicle (index)
        /// </summary>
        public int[] Capacities { get; private set; }

        /// <summary>
        /// Total load of each vehicle - must not succeed it's capacity
        /// </summary>
        public int[] FreeCapacities { get; private set; }

        /// <summary>
        /// The start end endpoint of each vehicle
        /// </summary>
        public Request Depot { get; private set; }

        /// <summary>
        /// Cost matrix representing the cost of travelling between two requests
        /// </summary>
        public long[,] CostMatrix { get; private set; }

        /// <summary>
        /// Number of available vehicles
        /// </summary>
        public int VehicleCount { get; private set; }

        /// <summary>
        /// The solution currently used for assigning requests to vehicles
        /// </summary>
        public Solution Solution {
            get => _solution;
            set {
                _solution = value;
                UpdateNextRequestQueues();
            }
        }
        private Solution _solution;

        // contains the next requests for each vehicle
        private Queue<int>[] nextRequests;

        public WorldState(int vehicles, Request depot, Request[] knownRequests, VehicleType[] vehicleTypes) {
            History = new Dictionary<int, Request>();
            VehicleCount = vehicles;
            CurrentRequests = new int[vehicles];
            Depot = depot;
            nextRequests = new Queue<int>[vehicles];

            // Initialize queues
            for(int i = 0; i < nextRequests.Length; i++) {
                nextRequests[i] = new Queue<int>();
            }

            // Initialize the location of every vehicle with the depot
            for(int i = 0; i < vehicles; i++) {
                CurrentRequests[i] = depot.Id;
            }

            // Add known requests
            for(int i = 0; i < knownRequests.Length; i++) {
                KnownRequests.Add(knownRequests[i].Id, knownRequests[i]);
            }

            CostMatrix = CalculateCostMatrix();

            // Handle vehicles
            var tempCapacities = new List<int>();

            foreach(var vehicleType in vehicleTypes) {
                for(int i = 0; i < vehicleType.VehicleCount; i++) {
                    tempCapacities.Add(vehicleType.Capacity);
                }
            }

            Capacities = tempCapacities.ToArray();
            FreeCapacities = tempCapacities.ToArray();
        }

        /// <summary>
        /// Add a new request
        /// </summary>
        /// <param name="request"></param>
        public void AddRequest(Request request) {
            KnownRequests.Add(request.Id, request);
            CostMatrix = CalculateCostMatrix();
        }

        public bool TryCommitNextRequest(int vehicle, out int nextRequest) {
            nextRequest = GetNextRequest(vehicle);

            // Check if there is a request available
            if (nextRequest < 0)
                return false;

            return CommitRequest(vehicle, nextRequest);
        }

        /// <summary>
        /// Returns the next request a vehicle must serve according to the currently accepted solution
        /// </summary>
        /// <param name="vehicle"></param>
        /// <returns>The id of the next request or -1 if there are no next requests the given vehicle</returns>
        private int GetNextRequest(int vehicle) {
            if(nextRequests[vehicle].Count < 1) { // nothing to do...
                return -1;
            }

            return nextRequests[vehicle].Dequeue();
        }

        /// <summary>
        /// Updates the request queues of each vehicle to represent the current solution
        /// </summary>
        private void UpdateNextRequestQueues() {
            for(int vehicle = 0; vehicle < VehicleCount; vehicle++) {
                // clear current queue
                nextRequests[vehicle].Clear();

                foreach(var request in Solution.Data[vehicle].Data) {
                    nextRequests[vehicle].Enqueue(request);
                }
            }
        }

        /// <summary>
        /// Commit a request to a vehicle. Cannot be undone
        /// </summary>
        /// <param name="vehicle"></param>
        /// <param name="request"></param>
        /// <returns>True if the commit was successful</returns>
        private bool CommitRequest(int vehicle, int request) {
            if(request != 0 && KnownRequests.ContainsKey(request)) { // dont mind the depot
                CurrentRequests[vehicle] = request; // assign request to vehicle
                FreeCapacities[vehicle] -= KnownRequests[request].Amount; // update available vehicle capacity
                KnownRequests[request].Vehicle = vehicle; // assign vehicle to request
                History.Add(KnownRequests[request].Id, KnownRequests[request]); // add request to history
                KnownRequests.Remove(request); // remove request from known request since it is already assigned to a vehicle
                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a <see cref="Problem"/> based on the information contained in the <see cref="WorldState"/>
        /// </summary>
        /// <returns></returns>
        public Problem ToProblem() {
            var requests = KnownRequests.Values.ToArray();
            var currentRequests = CurrentRequests.Where(x => x != Depot.Id).ToArray(); // get each request where a vehicle is positioned
            var mapping = new int[requests.Length + currentRequests.Length + 1]; // mind the depot
            mapping[0] = 0; // depot
            requests.Select(r => r.Id).ToArray().CopyTo(mapping, 1);
            currentRequests.CopyTo(mapping, requests.Length + 1); // mind the depot

            var start = CurrentRequests.Select(x => Array.IndexOf(mapping, x)).ToArray(); // indices of the starting requests

            var reducedCostMatrix = CreateReducedCostMatrix(mapping);

            return new Problem(
                requests,
                VehicleCount,
                FreeCapacities,
                start,
                reducedCostMatrix,
                mapping
                );
        }

        /// <summary>
        /// Evaluates a given <see cref="Solution"/> based on the current world state
        /// </summary>
        /// <param name="solution"></param>
        /// <returns></returns>
        public double EvaluateSolution(Solution solution) {
            var totalCost = 0.0;
            var visited = new bool[KnownRequests.Count() + History.Count()];

            for (int vehicle = 0; vehicle < VehicleCount; vehicle++) {
                var routeCost = 0.0;
                var capacity = Capacities[vehicle];
                var load = 0;
                var lastRequest = 0; // start at depot

                // Finished requests
                foreach (var entry in History) {
                    if (entry.Value.Vehicle == vehicle) {
                        var request = entry.Value;
                        visited[request.Id - 1] = true;
                        load += request.Amount;

                        // Violated capacity constraint
                        if (load > capacity)
                            return -1;

                        routeCost += CostMatrix[lastRequest, request.Id];
                        lastRequest = request.Id;
                    }
                }

                // Planned routes
                foreach (var request in solution.Data[vehicle].Data) {
                    // solution is deprecated
                    if(!KnownRequests.ContainsKey(request)) {
                        return -1;
                    }

                    var req = KnownRequests[request];
                    visited[request - 1] = true;
                    load += req.Amount;

                    // Violated capacity constraint
                    if (load > capacity)
                        return -1;

                    routeCost += CostMatrix[lastRequest, request];
                    lastRequest = request;
                }

                // Drive back to the depot
                routeCost += CostMatrix[lastRequest, 0];

                totalCost += routeCost;
            }

            // Check if every request has been visited
            if (visited.Contains(false))
                return -1;

            return totalCost;// + emissions;
        }

        /// <summary>
        /// Evaluates the currently used <see cref="Solution"/>
        /// </summary>
        /// <returns></returns>
        public double EvaluateCurrentSolution() {
            var totalCost = 0.0;
            var visited = new bool[KnownRequests.Count() + History.Count()];

            for (int vehicle = 0; vehicle < VehicleCount; vehicle++) {
                var routeCost = 0.0;
                var capacity = Capacities[vehicle];
                var load = 0;
                var lastRequest = 0; // start at depot

                // Finished requests
                foreach (var entry in History) {
                    if (entry.Value.Vehicle == vehicle) {
                        var request = entry.Value;
                        visited[request.Id - 1] = true;
                        load += request.Amount;

                        // Violated capacity constraint
                        if (load > capacity)
                            return -1;

                        routeCost += CostMatrix[lastRequest, request.Id];
                        lastRequest = request.Id;
                    }
                }

                // Planned routes
                foreach (var request in nextRequests[vehicle]) {
                    // solution is deprecated
                    if (!KnownRequests.ContainsKey(request)) {
                        return -1;
                    }

                    var req = KnownRequests[request];
                    visited[request - 1] = true;
                    load += req.Amount;

                    // Violated capacity constraint
                    if (load > capacity)
                        return -1;

                    routeCost += CostMatrix[lastRequest, request];
                    lastRequest = request;
                }

                // Drive back to the depot
                routeCost += CostMatrix[lastRequest, 0];

                totalCost += routeCost;
            }

            // Check if every request has been visited
            if (visited.Contains(false))
                return -1;

            return totalCost;// + emissions;
        }

        public Solution GetFinalSolution() {
            var solution = new Solution(VehicleCount);
            var routes = new List<int>[VehicleCount];

            // init
            for(int i = 0; i < routes.Length; i++) {
                routes[i] = new List<int>();
            }

            // Recreate routes from history
            foreach(var entry in History) {
                var request = entry.Value;
                routes[request.Vehicle].Add(request.Id);
            }

            // Add routes to solution
            for(int i = 0; i < routes.Length; i++) {
                solution.AddRoute(i, routes[i].ToArray());
            }

            return solution;
        }

        /// <summary>
        /// Checks if the given solution is better than the current one
        /// </summary>
        /// <param name="solution"></param>
        /// <returns></returns>
        public bool TrySetNewSolution(Solution solution) {
            var cost = EvaluateSolution(solution);

            // infeasible - should not happen
            if (cost < 0)
                return false;

            // Handle first solution
            if (Solution == null) {
                Solution = solution;
                return true;
            }

            var oldCost = EvaluateCurrentSolution(); // re-evaluate in case of changes

            if((oldCost < 0 && cost >= 0) || (cost < oldCost)) {
                Solution = solution;
                return true;
            }

            return false;
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
                        matrix[fromNode, toNode] = 0;
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
