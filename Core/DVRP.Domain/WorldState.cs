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
        public static Solution Solution {
            get => _solution;
            set {
                solutionMutex.WaitOne();
                _solution = value;
                solutionMutex.ReleaseMutex();
            }
        }

        private static Mutex solutionMutex = new Mutex(false);
        private static Solution _solution;

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
            FreeCapacities = capacities.ToArray();
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
        /// <returns>True if the commit was successful</returns>
        public bool CommitRequest(int vehicle, int request) {
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
            var mapping = new int[requests.Length + 1]; // mind the depot
            mapping[0] = 0; // depot
            requests.Select(r => r.Id).ToArray().CopyTo(mapping, 1);

            var reducedCostMatrix = CreateReducedCostMatrix(mapping);

            /*Console.WriteLine("-------------------------------");
            Console.WriteLine($"Requests: {requests.Length}");
            Console.WriteLine(">>> Cost Matrix");
            var sb = new StringBuilder();

            for(int i = 0; i < reducedCostMatrix.GetLength(0); i++) {    
                for(int j = 0; j < reducedCostMatrix.GetLength(0); j++) {
                    sb.Append($"{reducedCostMatrix[i,j]}\t");
                }
                sb.AppendLine();
            }
            sb.AppendLine();
            Console.WriteLine(sb.ToString());*/

            return new Problem(
                requests,
                VehicleCount,
                FreeCapacities,
                CurrentRequests,
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

            // TODO check if every request is serviced

            for (int vehicle = 0; vehicle < VehicleCount; vehicle++) {
                var routeCost = 0.0;
                var capacity = Capacities[vehicle];
                var load = 0;
                var lastRequest = 0; // start at depot
                // TODO add green-vrp stuff here

                // Finished requests
                foreach (var entry in History) {
                    if (entry.Value.Vehicle == vehicle) {
                        var request = entry.Value;
                        load += request.Amount;

                        // Violated capacity constraint
                        if (load > capacity)
                            return -1;

                        routeCost += CostMatrix[lastRequest, request.Id];
                        lastRequest = request.Id;
                    }
                }

                // Planned routes
                foreach (var request in solution.Data[vehicle]) { // solution can be modified!!
                    var req = GetRequest(request);
                    load += req.Amount;

                    // Violated capacity constraint
                    if (load > capacity)
                        return -1;

                    routeCost += CostMatrix[lastRequest, request];
                    lastRequest = request;
                }

                totalCost += routeCost;
            }

            return totalCost;
        }

        /// <summary>
        /// Evaluates the currently used <see cref="Solution"/>
        /// </summary>
        /// <returns></returns>
        public double EvaluateCurrentSolution() {
            solutionMutex.WaitOne(); // solution must not be changed during execution
            var result = EvaluateSolution(Solution);
            solutionMutex.ReleaseMutex();

            return result;
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
