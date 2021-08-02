using DVRP.Domain;
using System;
using System.Collections.Generic;
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
                pheromoneMatrix = InitPheromoneMatrix(problem.Requests.Length + problem.VehicleCount, initialPheromoneValue);
            } else if(pheromoneMatrix.GetLength(0) != problem.VehicleCount + problem.Requests.Length) {
                // Adjust pheromone matrix to changed requests
                pheromoneMatrix = TransformPheromoneMatrix(problem.VehicleCount, problem.Requests.Length, pheromoneMatrix, initialPheromoneValue);
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
                    var from = bestSolution.Route[i] - 1;
                    var to = bestSolution.Route[i + 1] - 1;
                    
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

        private static double[,] InitPheromoneMatrix(int locationNumber, double initialPheromoneValue) {
            var matrix = new double[locationNumber, locationNumber];

            for(int i = 0; i < locationNumber; i++) {
                for(int j = 0; j < locationNumber; j++) {
                    matrix[i, j] = initialPheromoneValue;
                }
            }

            return matrix;
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
        private static double[,] TransformPheromoneMatrix(int vehicleCount, int requestCount, double[,] pheromoneMatrix, double initialPheromoneValue) {
            var length = vehicleCount + requestCount;

            if (pheromoneMatrix.Length == length) {
                return pheromoneMatrix;
            }

            // Only works with more requests, request must not be less than in previous iterations
            var matrix = new double[length, length];

            for (int i = 0; i < length; i++) {
                for (int j = 0; j < length; j++) {
                    if (i >= pheromoneMatrix.Length || j >= pheromoneMatrix.Length) {
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
