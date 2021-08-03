using DVRP.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVRP.Optimizer.ACS
{
    public class ACSSolver
    {
        private static double[,] pheromoneMatrix;

        public static DVRP.Domain.Solution Solve(Problem problem, int computationTime, int antNumber, double pheromoneEvaporation = 0.5, double pheromoneImportance = 0.5, double initialPheromoneValue = 0.2) {
            // init pheromone level
            if(pheromoneMatrix == null) {
                // matrix needs to include dummy depots for each vehicle
                pheromoneMatrix = InitPheromoneMatrix(problem, initialPheromoneValue);
            } else {
                // May be necessary to adjust pheromone matrix due to new requests
                pheromoneMatrix = TransformPheromoneMatrix(problem, pheromoneMatrix, initialPheromoneValue);
            }

            Solution bestSolution = null;
            var costMatrix = TransformDistanceMatrix(problem);

            while (0 < computationTime) { // TODO computation time
                for(int k = 0; k < antNumber; k++) {
                    var ant = new Ant(problem, pheromoneMatrix, costMatrix, pheromoneEvaporation, pheromoneImportance, initialPheromoneValue);
                    //Console.WriteLine($"[Ant-{k}] FindSolution...");
                    var solution = ant.FindSolution();

                    if(bestSolution == null) {
                        bestSolution = solution;
                    }

                    if(solution.IsValid() && solution.Cost < bestSolution.Cost) {
                        bestSolution = solution;
                        //Console.WriteLine($"[Ant-{k}] Found new best solution");
                    }
                }

                // update global pheromone trail
                for(int i = 0; i < bestSolution.Route.Length - 1; i++) {
                    var from = bestSolution.Route[i] - 1; // exclude depot
                    var to = bestSolution.Route[i + 1] - 1;

                    from = ToPheromoneIndex(from, problem);
                    to = ToPheromoneIndex(to, problem);

                    // update every edge between each node (customer)
                    pheromoneMatrix[from, to] =
                        (1 - pheromoneEvaporation) * pheromoneMatrix[from, to] +
                        pheromoneEvaporation / bestSolution.Cost;
                }

                computationTime--; // this is just temporary
            }
            var convertedSolution = bestSolution.ConvertToDomainSolution();
            convertedSolution.ApplyMapping(problem.Mapping);
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine($"Best solution: {bestSolution.Cost}");
            Console.WriteLine(convertedSolution);

            return convertedSolution;
        }

        /// <summary>
        /// Creates a pheromone matrix with a fixed pheromone value
        /// </summary>
        /// <param name="locationNumber"></param>
        /// <param name="initialPheromoneValue"></param>
        /// <returns></returns>
        private static double[,] InitPheromoneMatrix(Problem problem, double initialPheromoneValue) {
            var length = problem.VehicleCount + problem.Requests.Length + 1; // depot
            var matrix = new double[length, length];

            for(int i = 0; i < length; i++) {
                for(int j = 0; j < length; j++) {
                    matrix[i, j] = initialPheromoneValue;
                }
            }

            return matrix;
        }

        /// <summary>
        /// Maps the index of a problem to the global pheromone matrix
        /// </summary>
        /// <param name="index"></param>
        /// <param name="problem"></param>
        /// <returns></returns>
        public static int ToPheromoneIndex(int index, Problem problem) {
            if (index <= problem.VehicleCount) {
                return index;
            } else {
                // Get the id of the request at index and add vehicle count and depot to get the correct index on the pheromone matrix
                var res = problem.Mapping[index - problem.VehicleCount - 1] + problem.VehicleCount;
                return res;
            }
        }

        /// <summary>
        /// Transforms the cost matrix of a problem to the form the ACS is using:
        /// 0: depot
        /// 1-n: current position of the vehicles
        /// n-m: requests
        /// </summary>
        /// <param name="problem"></param>
        /// <returns></returns>
        private static long[,] TransformDistanceMatrix(Problem problem) {
            var length = problem.VehicleCount + problem.Requests.Length + 1; // mind the depot
            var matrix = new long[length, length];

            // dummy depots (starting positions) are inserted after the real depot
            // therefore the index of the original cost matrix is vehicleCount + 1 (depot) lower than in the new matrix
            var requestOffset = problem.VehicleCount + 1;

            var from = 0;
            var to = 0;
            for (int i = 0; i < length; i++) {
                if (i > 0) { // depot needs no mapping
                    if (i < requestOffset) { // dummy depot
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
                    } else {
                        to = 0;
                    }

                    if(from == to) {
                        matrix[i, j] = 0;
                    } else {
                        matrix[i, j] = problem.CostMatrix[from, to];
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
        private static double[,] TransformPheromoneMatrix(Problem problem, double[,] pheromoneMatrix, double initialPheromoneValue) {
            var currentLength = pheromoneMatrix.GetLength(0);

            // Check if there is a new request (unknown id)
            var maxId = 0; // The highest id of all current requests

            for(int i = 0; i < problem.Requests.Length; i++) {
                if(problem.Requests[i].Id > maxId) {
                    maxId = problem.Requests[i].Id;
                }
            }

            // Transform maxId to the id it would have in the pheromone matrix
            var maxIdx = maxId + problem.VehicleCount;

            // The matrix is big enough for the highest id => no unhandled request
            if (currentLength > maxIdx) {
                return pheromoneMatrix;
            }

            // The new length is the current length plus the difference to fit the highest id
            var newLength = currentLength + (maxIdx - (currentLength - 1));
            var matrix = new double[newLength, newLength];

            for(int i = 0; i < newLength; i++) {
                for(int j = 0; j < newLength; j++) {
                    if(i < currentLength && j < currentLength) { // copy
                        matrix[i, j] = pheromoneMatrix[i, j];
                    } else {
                        matrix[i, j] = initialPheromoneValue;
                    }
                }
            }

            return matrix;
        }
    }
}
