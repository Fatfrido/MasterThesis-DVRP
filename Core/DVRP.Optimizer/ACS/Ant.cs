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
        private Problem problem;

        public Ant(Problem problem, double initialPheromoneValue, double[,] pheromoneMatrix = null, double pheromoneEvaporation = 0.5, double pheromoneImportance = 0.5) {
            this.problem = problem;
            this.initialPheromoneValue = initialPheromoneValue;
            costMatrix = TransformDistanceMatrix(problem);
            this.pheromoneEvaporation = pheromoneEvaporation;
            this.pheromoneImportance = pheromoneImportance;

            // Take existing pheromone matrix or create a new one
            this.pheromoneMatrix = pheromoneMatrix != null 
                ? TransformPheromoneMatrix(problem.VehicleCount, problem.Requests.Length, pheromoneMatrix, initialPheromoneValue) 
                : CreatePheromoneMatrix(problem.VehicleCount, problem.Requests.Length, initialPheromoneValue);
        }

        /// <summary>
        /// Calculates a feasible solution
        /// </summary>
        /// <returns></returns>
        public Solution FindSolution() {
            var initialSolution = BuildInitialSolution(problem);
            //Console.WriteLine("Build initial solution");
            initialSolution.Cost = Evaluate(initialSolution, problem);

            while(initialSolution.Cost < 0) { // solution must be feasible -> improve BuildInitialSolution to only return feasible solutions?
                //Console.WriteLine("Build intial solution");
                initialSolution = BuildInitialSolution(problem);
            }
            //Console.WriteLine("Evaluating solution");
            return LocalSearch(initialSolution, problem, 10);
        }

        private long[,] TransformDistanceMatrix(Problem problem) {
            var length = problem.VehicleCount + problem.Requests.Length + 1; // mind the depot
            var matrix = new long[length, length];

            // dummy depots (starting positions) are inserted after the real depot
            // therefore the index of the original cost matrix is vehicleCount + 1 (depot) lower than in the new matrix
            var requestOffset = problem.VehicleCount + 1;

            var from = 0;
            var to = 0;
            for (int i = 0; i < length; i++) {
                if(i > 0) { // depot needs no mapping
                    if(i < requestOffset) { // dummy depot
                        from = problem.Start[i - 1];
                    } else { // request
                        from = i - requestOffset + 1; // mind depot in original matrix
                    }
                }

                for (int j = 0; j < length; j++) {
                    if (j > 0) { // depot needs no mapping
                        if (j < requestOffset) { // dummy depot
                            to = problem.Start[j - 1];
                        } else { // request
                            to = j - requestOffset + 1; // mind depot in original matrix
                        }
                    }

                    matrix[i, j] = problem.CostMatrix[from, to];
                }
            }

            /*Console.WriteLine("-----------------------------------------------------");
            Console.WriteLine("Original matrix:");
            Console.WriteLine(PrintMatrix(problem.CostMatrix));

            Console.WriteLine("Transformed matrix:");
            Console.WriteLine(PrintMatrix(matrix));*/

            return matrix;
        }

        /*private string PrintMatrix(long[,] matrix) {
            var sb = new StringBuilder();

            for(int i = 0; i < matrix.GetLength(0); i++) {
                for(int j = 0; j < matrix.GetLength(0); j++) {
                    sb.Append($"{matrix[i,j]}\t");
                }
                sb.AppendLine();
            }
            sb.AppendLine();

            return sb.ToString();
        }*/

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
        /// Constructs a single solution
        /// </summary>
        /// <param name="vehicleCount"></param>
        /// <returns></returns>
        private Solution BuildInitialSolution(Problem problem) {
            var solution = new Solution(problem.VehicleCount);
            var route = new List<int>();
            var length = problem.Requests.Length + problem.VehicleCount; // the total length of a solution (includes dummy depots but not the real depot)
            
            var currentRequestIdx = 0;
            var nextRequest = 0;

            var vehicle = 0;
            var vehicleCapacity = problem.VehicleCapacity[vehicle];

            // store already visited locations
            var status = new bool[length];
            
            // Add start dummy depot
            status[0] = true;
            route.Add(currentRequestIdx);

            for(int i = 0; i < length - 1; i++) {
                // Find valid options
                var options = new List<int>();

                for(int j = 0; j < status.Length; j++) {
                    if(status[j] == false) { // each request is visited exactly once 
                        // check capacity constraint for normal requests (not dummy depots)
                        //if(j < problem.VehicleCount || vehicleCapacity - problem.Requests[j - problem.VehicleCount].Amount >= 0) {
                        options.Add(j);
                        //}
                    }
                }

                // Calculate attractivity for each option
                var attractivenessSum = 0.0;
                var attractiveness = new Dictionary<int, double>(options.Count());

                foreach (var option in options) {
                    var generalAttractiveness = 1.0 / costMatrix[currentRequestIdx + 1, option + 1]; // cost matrix includes the real depot at index 0
                    var pheromoneAttractiveness = Math.Pow(pheromoneMatrix[currentRequestIdx, option], pheromoneImportance);
                    attractiveness[option] = generalAttractiveness * pheromoneAttractiveness;
                    attractivenessSum += attractiveness[option];
                }

                // calculate probability
                var probabilities = new Dictionary<int, double>(options.Count());

                foreach (var option in options) {
                    probabilities[option] = attractiveness[option] / attractivenessSum;
                }

                nextRequest = SelectRandomCustomer(probabilities);
                status[nextRequest] = true;
                route.Add(nextRequest);

                // Check if a dummy depot has been selected
                if(nextRequest < problem.VehicleCount) {
                    vehicle = nextRequest;
                    vehicleCapacity = problem.VehicleCapacity[vehicle];
                } else { // update available vehicle capacity
                    vehicleCapacity -= problem.Requests[nextRequest - problem.VehicleCount].Amount;
                }

                currentRequestIdx = nextRequest;
            }

            solution.Route = route.Select(x => x + 1).ToArray(); // increase each index by 1 to match the global cost matrix

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
        private Solution LocalSearch(Solution initialSolution, Problem problem, int maxComputationTime) {
            // find best solution with local search
            var bestSolution = initialSolution;
            var iterations = 0; //TODO fix computation time

            while(iterations < maxComputationTime) {
                // Try to place a request at a different position
                var fromIndex = random.Next(1, bestSolution.Route.Length - 1);
                var toIndex = random.Next(1, bestSolution.Route.Length - 1);

                var solution = bestSolution.MoveRequest(fromIndex, toIndex);
                solution.Cost = Evaluate(solution, problem);

                if(solution.IsValid() && solution.Cost < bestSolution.Cost) {
                    bestSolution = solution;
                    Console.WriteLine($"[Ant] Found personal best: {bestSolution.Cost}");
                }

                iterations++;
            }

            return bestSolution;
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
        private double Evaluate(Solution solution, Problem problem) {
            // infeasible solution if it does not start with a dummy depot
            if(! solution.IsDummyDepot(0)) {
                return -1;
            }

            // handle first dummy depot
            var vehicle = solution.Route[0];
            var currCapacity = problem.VehicleCapacity[0];
            var cost = 0.0;

            for(int i = 1; i < solution.Route.Length; i++) {
                // check if current request is a dummy depot
                if(solution.IsDummyDepot(i)) {
                    // change current vehicle
                    vehicle = solution.Route[i];
                    currCapacity = problem.VehicleCapacity[vehicle - 1]; // capacities start with 0
                } else {
                    var requestIndex = solution.GetRealIndex(solution.Route[i] - 1);
                    currCapacity -= problem.Requests[requestIndex].Amount;

                    // check constraints
                    if(currCapacity < 0) {
                        return -1;
                    }

                    cost += costMatrix[i, i + 1]; // cost matrix has depot at index 0
                }
            }

            solution.Cost = cost;
            return cost;
        }

        /// <summary>
        /// Calculates a swap2 neighborhood
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="problem"></param>
        /// <returns></returns>
        private IEnumerable<Solution> GetNeighborhood(Solution solution, Problem problem) { // TODO add time limit
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
