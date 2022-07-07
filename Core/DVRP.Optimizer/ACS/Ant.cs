using DVRP.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVRP.Optimizer.ACS
{
    public class Ant
    {
        private long[,] costMatrix;
        private PheromoneMatrix pheromoneMatrix;
        private double pheromoneImportance;
        private Random random = new Random();
        private Problem problem;
        private int localSearchIterations;
        private double exploitationImportance;

        public Ant(Problem problem, PheromoneMatrix pheromoneMatrix, long[,] costMatrix, double pheromoneImportance, int localSearchIterations, double exploitationImportance)
        {
            this.problem = problem;
            this.pheromoneMatrix = pheromoneMatrix;
            this.costMatrix = costMatrix;
            this.pheromoneImportance = pheromoneImportance;
            this.localSearchIterations = localSearchIterations;
            this.exploitationImportance = exploitationImportance;
        }

        /// <summary>
        /// Calculates a feasible solution
        /// </summary>
        /// <returns></returns>
        public Solution FindSolution()
        {
            var initialSolution = BuildInitialSolution(problem);
            initialSolution.Cost = Evaluate(initialSolution, problem);

            return LocalSearch(initialSolution, problem, localSearchIterations);
        }

        /// <summary>
        /// Constructs a single solution
        /// </summary>
        /// <param name="vehicleCount"></param>
        /// <returns></returns>
        private Solution BuildInitialSolution(Problem problem)
        {
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

            for (int i = 0; i < length - 1; i++)
            {
                // Find valid options
                var options = new List<int>();

                for (int j = 0; j < status.Length; j++)
                {
                    if (status[j] == false)
                    { // each request is visited exactly once 
                        // check capacity constraint for normal requests (not dummy depots)
                        if (j < problem.VehicleCount || vehicleCapacity - problem.Requests[j - problem.VehicleCount].Amount >= 0)
                        {
                            options.Add(j);
                        }
                    }
                }

                if (options.Count < 1)
                { // no more feasible option available
                    return BuildInitialSolution(problem);
                }

                // Calculate attractivity for each option
                var attractivenessSum = 0.0;
                var attractiveness = new Dictionary<int, double>(options.Count());

                foreach (var option in options)
                {
                    var cost = costMatrix[currentRequestIdx + 1, option + 1];

                    if (cost <= 0) // make sure to not divide by 0
                        cost = 100; // if this value is too low, stack overflow might occur

                    var generalAttractiveness = 1.0 / cost; // cost matrix includes the real depot at index 0
                    var pheromoneAttractiveness = Math.Pow(pheromoneMatrix[currentRequestIdx, option], pheromoneImportance);
                    attractiveness[option] = generalAttractiveness * pheromoneAttractiveness;
                    attractivenessSum += attractiveness[option];
                }

                // calculate probability
                var probabilities = new Dictionary<int, double>(options.Count());
                var maxP = 0.0;
                var maxOption = 0;

                foreach (var option in options)
                {
                    var p = attractiveness[option] / attractivenessSum;
                    probabilities[option] = p;

                    if (p > maxP)
                    {
                        maxP = p;
                        maxOption = option;
                    }
                }

                // Select the option with the highest probability with probability exploitationImportance (q)
                if (random.NextDouble() < exploitationImportance)
                {
                    nextRequest = maxOption;
                }
                else
                {
                    nextRequest = SelectRandomCustomer(probabilities);
                }

                status[nextRequest] = true;
                route.Add(nextRequest);

                pheromoneMatrix.LocalUpdate(currentRequestIdx, nextRequest, problem);

                // Check if a dummy depot has been selected
                if (nextRequest < problem.VehicleCount)
                {
                    vehicle = nextRequest;
                    vehicleCapacity = problem.VehicleCapacity[vehicle];
                }
                else
                { // update available vehicle capacity
                    vehicleCapacity -= problem.Requests[nextRequest - problem.VehicleCount].Amount;
                }

                currentRequestIdx = nextRequest;
            }

            solution.Route = route.Select(x => x + 1).ToArray(); // increase each index by 1 to match the global cost matrix

            return solution;
        }

        /// <summary>
        /// Applies a simple local search to a solution and returns the best result
        /// </summary>
        /// <param name="initialSolution"></param>
        /// <param name="problem"></param>
        /// <param name="maxComputationTime"></param>
        /// <returns></returns>
        private Solution LocalSearch(Solution initialSolution, Problem problem, int iterations)
        {
            // find best solution with local search
            var bestSolution = initialSolution;
            var i = 0;

            while (i < iterations)
            {
                // Try to place a request at a different position
                var fromIndex = random.Next(1, bestSolution.Route.Length - 1);
                var toIndex = random.Next(1, bestSolution.Route.Length - 1);

                var solution = bestSolution.MoveRequest(fromIndex, toIndex);
                solution.Cost = Evaluate(solution, problem);

                if (solution.IsValid() && solution.Cost < bestSolution.Cost)
                {
                    bestSolution = solution;
                    //Console.WriteLine($"[Ant] Found personal best: {bestSolution.Cost}");
                }

                i++;
            }

            return bestSolution;
        }

        /// <summary>
        /// Selects a random customer with individual probabilities
        /// </summary>
        /// <param name="probabilities"></param>
        /// <returns></returns>
        // see: https://stackoverflow.com/questions/38086513/selecting-random-item-from-list-given-probability-of-each-item
        private int SelectRandomCustomer(IDictionary<int, double> probabilities)
        {
            // calc universial probability
            var universialProbability = probabilities.Sum(pair => pair.Value);

            // pick random number between 0 and universialProbability
            var rand = random.NextDouble() * universialProbability;

            double sum = 0;
            foreach (var p in probabilities)
            {
                // loop until the random number is less than our cumulative probability
                if (rand <= (sum = sum + p.Value))
                {
                    return p.Key;
                }
            }

            // should never get here
            return -1;
        }

        /// <summary>
        /// Evaluates a solution
        /// </summary>
        /// <param name="solution"></param>
        /// <param name="problem"></param>
        /// <returns></returns>
        private double Evaluate(Solution solution, Problem problem)
        {
            // infeasible solution if it does not start with a dummy depot
            if (!solution.IsDummyDepot(0))
            {
                return -1;
            }

            // handle first dummy depot
            var vehicle = solution.Route[0];
            var currCapacity = problem.VehicleCapacity[vehicle - 1];
            var cost = 0.0;
            var lastRequest = solution.Route[0];
            var visited = new bool[problem.Requests.Length];

            for (int i = 1; i < solution.Route.Length; i++)
            {
                // check if current request is a dummy depot
                if (solution.IsDummyDepot(i))
                {
                    // return current vehicle back to depot
                    cost += costMatrix[lastRequest, 0];

                    // change current vehicle
                    vehicle = solution.Route[i];
                    currCapacity = problem.VehicleCapacity[vehicle - 1]; // capacities start with 0
                    lastRequest = vehicle;
                }
                else
                {
                    var requestIndex = solution.GetRealIndex(solution.Route[i] - 1);
                    currCapacity -= problem.Requests[requestIndex].Amount;

                    // check constraints
                    if (currCapacity < 0)
                    {
                        return -1;
                    }

                    cost += costMatrix[lastRequest, solution.Route[i]]; // cost matrix has depot at index 0
                    lastRequest = solution.Route[i];
                }
            }

            // return last vehicle to depot
            cost += costMatrix[lastRequest, 0];

            solution.Cost = cost;
            return cost;
        }
    }
}
