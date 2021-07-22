using DVRP.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVRP.Optimizer.ACS
{
    public class Ant
    {
        // local pheromone matrix
        // initial pheromone value
        // cost matrix
        private long[,] costMatrix;
        private double[,] pheromoneMatrix;
        private double initialPheromoneValue;
        private double pheromoneImportance; // 0 <= pheromoneImportance <= 1
        private Random random = new Random();
        private double pheromoneEvaporation = 0.5; // 0 <= pheromoneEvaporation <= 1
        private WorldState problem;

        public Ant(WorldState problem, double initialPheromoneValue, double[,] pheromoneMatrix = null, double pheromoneEvaporation = 0.5, double pheromoneImportance = 0.5) {
            this.problem = problem;
            this.initialPheromoneValue = initialPheromoneValue;
            costMatrix = TransformDistanceMatrix(problem.VehicleCount, problem.KnownRequests.Count, problem.CostMatrix);
            this.pheromoneEvaporation = pheromoneEvaporation;
            this.pheromoneImportance = pheromoneImportance;

            // Take existing pheromone matrix or create a new one
            this.pheromoneMatrix = pheromoneMatrix != null 
                ? TransformPheromoneMatrix(problem.VehicleCount, problem.KnownRequests.Count, pheromoneMatrix, initialPheromoneValue) 
                : CreatePheromoneMatrix(problem.VehicleCount, problem.KnownRequests.Count, initialPheromoneValue);
        }

        /// <summary>
        /// Calculates a feasible solution
        /// </summary>
        /// <returns></returns>
        public Solution FindSolution() {
            var initialSolution = BuildInitialSolution(problem.VehicleCount, problem.KnownRequests.Count, 0);
            Console.WriteLine("Build initial solution");
            initialSolution.Cost = Evaluate(initialSolution, problem);

            while(initialSolution.Cost < 0) { // solution must be feasible -> improve BuildInitialSolution to only return feasible solutions
                Console.WriteLine("Build intial solution");
                initialSolution = BuildInitialSolution(problem.VehicleCount, problem.KnownRequests.Count, 0);
            }
            Console.WriteLine("Evaluating solution");
            return LocalSearch(initialSolution, problem, 1);
        }

        private long[,] TransformDistanceMatrix(int vehicleCount, int requestCount, long[,] distanceMatrix) {
            var length = vehicleCount + requestCount;

            var matrix = new long[length, length];

            for (int i = 0; i < length; i++) {
                for (int j = 0; j < length; j++) {
                    if(i <= vehicleCount && j > vehicleCount) {
                        matrix[i, j] = distanceMatrix[0, j];
                    } else if (j <= vehicleCount && i > vehicleCount) {
                        matrix[i, j] = distanceMatrix[i, 0];
                    } else if(i <= vehicleCount && j <= vehicleCount) {
                        matrix[i, j] = 1; // distance between dummy depots
                    } else {
                        matrix[i, j] = distanceMatrix[i, j];
                    }
                }
            }

            return matrix;
        }

        /// <summary>
        /// Extends the original pheromone matrtix if necessary
        /// </summary>
        /// <param name="vehicleCount"></param>
        /// <param name="requestCount"></param>
        /// <param name="pheromoneMatrix"></param>
        /// <param name="initialPheromoneValue"></param>
        /// <returns></returns>
        private double[,] TransformPheromoneMatrix(int vehicleCount, int requestCount, double[,] pheromoneMatrix, double initialPheromoneValue) {
            var length = vehicleCount + requestCount;

            if(pheromoneMatrix.Length == length) {
                return pheromoneMatrix;
            }

            // Only works with more requests, request must not be less than in previous iterations
            var matrix = new double[length, length];

            for(int i = 0; i < length; i++) {
                for(int j = 0; j < length; j++) {
                    if(i >= pheromoneMatrix.Length || j >= pheromoneMatrix.Length) {
                        matrix[i, j] = pheromoneMatrix[i, j];
                    } else {
                        matrix[i, j] = initialPheromoneValue;
                    }
                }
            }

            return matrix;
        }

        /// <summary>
        /// Creates a new pheromone matrix
        /// </summary>
        /// <param name="vehicleCount"></param>
        /// <param name="requestCount"></param>
        /// <param name="initialPheromoneValue"></param>
        /// <returns></returns>
        private double[,] CreatePheromoneMatrix(int vehicleCount, int requestCount, double initialPheromoneValue) {
            var length = vehicleCount + requestCount;
            var matrix = new double[length, length];

            for(int i = 0; i < length; i++) {
                for(int j = 0; j < length; j++) {
                    matrix[i, j] = initialPheromoneValue;
                }
            }

            return matrix;
        }

        /// <summary>
        /// Constructs a single feasible solution
        /// </summary>
        /// <param name="vehicleCount"></param>
        /// <returns></returns>
        private Solution BuildInitialSolution(int vehicleCount, int requestCount, int startRequest) {
            var solution = new Solution(vehicleCount);
            var route = new List<int>();
            var length = requestCount + vehicleCount; // the total length of a solution (includes dummy depots)

            // store already visited locations
            var status = new bool[length];
            var visitedLocationsNumber = 0;
            var currentCustomer = startRequest;

            // build solution
            while(visitedLocationsNumber < length) {
                // select next customer

                // find valid options
                List<int> options = new List<int>();
                for(int i = 0; i < status.Length; i++) {
                    if(status[i] == false) {
                        options.Add(i);
                    }
                }
                // TODO: capacity constraint

                // calculate attractivity
                var attractivenessSum = 0.0;
                var attractiveness = new Dictionary<int, double>(options.Count());

                foreach(var option in options) {
                    var generalAttractiveness = 1.0 / costMatrix[currentCustomer, option];
                    var pheromoneAttractiveness = Math.Pow(pheromoneMatrix[currentCustomer, option], pheromoneImportance);
                    attractiveness[option] = generalAttractiveness * pheromoneAttractiveness;
                    attractivenessSum += attractiveness[option];
                }

                // calculate probability
                var probabilities = new Dictionary<int, double>(options.Count());
                foreach(var option in options) {
                    probabilities[option] = attractiveness[option] / attractivenessSum;
                }

                var nextCustomer = SelectRandomCustomer(probabilities);
                route.Add(currentCustomer);

                // update local trail level
                UpdateTrailLevel(currentCustomer, nextCustomer);

                status[nextCustomer] = true;
                visitedLocationsNumber++;
                currentCustomer = nextCustomer;
            }

            solution.Route = route.ToArray();

            Console.WriteLine($"Initial solution: {solution}");
            return solution;
        }

        /// <summary>
        /// Applies a simple local search to a solution and returns the best result
        /// </summary>
        /// <param name="initialSolution"></param>
        /// <param name="problem"></param>
        /// <param name="maxComputationTime"></param>
        /// <returns></returns>
        private Solution LocalSearch(Solution initialSolution, WorldState problem, int maxComputationTime) {
            var solution = new Solution(problem.VehicleCount);

            // find best solution with local search
            var neighborhood = GetNeighborhood(solution, problem);
            var bestSolution = initialSolution;

            foreach(var neighbor in neighborhood) {
                if(neighbor.Cost < bestSolution.Cost) {
                    bestSolution = neighbor;
                }
            }

            Console.WriteLine($"Best solution: {bestSolution}");
            return solution;
        }

        /// <summary>
        /// Selects a random customer with individual probabilities
        /// </summary>
        /// <param name="probabilities"></param>
        /// <returns></returns>
        // see: https://stackoverflow.com/questions/38086513/selecting-random-item-from-list-given-probability-of-each-item
        private int SelectRandomCustomer(IDictionary<int, double> probabilities) {
            // calc universial probability
            var universialProbability = probabilities.Sum(pair => pair.Value);

            // pick random number between 0 and universialProbability
            var rand = random.NextDouble() * universialProbability;

            double sum = 0;
            foreach(var p in probabilities) {
                // loop until the random number is less than our cumulative probability
                if(rand <= (sum = sum + p.Value)) {
                    return p.Key;
                }
            }

            // should never get here
            return -1;
        }

        /// <summary>
        /// Updates the local pheromone matrix between two requests
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        private void UpdateTrailLevel(int from, int to) {
            pheromoneMatrix[from, to] = ((1 - pheromoneEvaporation) * pheromoneMatrix[from, to]) + 
                (pheromoneEvaporation * initialPheromoneValue);
        }

        /// <summary>
        /// Evaluates a solution
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="problem"></param>
        /// <returns></returns>
        private double Evaluate(Solution solution, WorldState problem) {
            // infeasible solution if it does not start with a dummy depot
            if(! solution.IsDummyDepot(0)) {
                return -1;
            }

            var vehicle = solution.Route[0];
            var currCapacity = problem.Capacities[0];
            var cost = 0.0;

            for(int i = 1; i < solution.Route.Length; i++) {
                // check if current location is a dummy depot
                if(solution.IsDummyDepot(i)) {
                    vehicle = solution.Route[i];
                    currCapacity = problem.Capacities[0];
                } else {
                    var requestIndex = solution.GetRealIndex(i);
                    currCapacity -= problem.KnownRequests[requestIndex].Amount;

                    // check constraints
                    if (currCapacity < 0) {
                        Console.WriteLine("Violated capacity constraint");
                        return -1;
                    }

                    var from = solution.GetRealIndex(i - 1);
                    cost += problem.CostMatrix[from, requestIndex];
                }
            }

            return cost;
        }

        /// <summary>
        /// Calculates a swap2 neighborhood
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="problem"></param>
        /// <returns></returns>
        private IEnumerable<Solution> GetNeighborhood(Solution solution, WorldState problem) { // TODO add time limit
            var neighbors = new List<Solution>();

            for(int i = 0; i < solution.Route.Length - 1; i++) {
                for(int j = i + 1; j < solution.Route.Length; j++) {
                    var neighbor = solution.Swap(i, j);
                    var cost = Evaluate(neighbor, problem);
                    if(cost >= 0) { // feasible solution
                        neighbor.Cost = cost;

                        // add to list of feasible neighbors
                        neighbors.Add(neighbor);
                    }
                }
            }

            return neighbors;
        }
    }
}
