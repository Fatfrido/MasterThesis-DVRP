using DVRP.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DVRP.Optimizer.ACS
{
    public class AntColonySystem : IPeriodicOptimizer
    {
        private PheromoneMatrix pheromoneMatrix;
        private int iterations;
        private int antNumber;
        private double pheromoneEvaporation;
        private double pheromoneImportance;
        private double initialPheromoneValue;
        private int localSearchIterations;
        private double pheromoneConservation;
        private double exploitationImportance;

        public AntColonySystem(int iterations, int antNumber, double pheromoneEvaporation, double pheromoneImportance, int localSearchIterations, double pheromoneConservation, double exploitationImportance) {
            this.iterations = iterations;
            this.antNumber = antNumber;
            this.pheromoneEvaporation = pheromoneEvaporation;
            this.pheromoneImportance = pheromoneImportance;
            this.localSearchIterations = localSearchIterations;
            this.pheromoneConservation = pheromoneConservation;
            this.exploitationImportance = exploitationImportance;
        }

        public Domain.Solution Solve(Problem problem) {
            // Update the initial pheromone value
            this.initialPheromoneValue = CalculateInitialPheromoneValue(problem);

            // init pheromone level
            if (pheromoneMatrix == null) {
                // matrix needs to include dummy depots for each vehicle
                pheromoneMatrix = new PheromoneMatrix(problem, initialPheromoneValue, pheromoneEvaporation, pheromoneConservation);
            } else {
                // May be necessary to adjust pheromone matrix due to new requests
                pheromoneMatrix.Update(problem);
            }

            Solution bestSolution = null;
            var costMatrix = TransformDistanceMatrix(problem);
            var remainingIterations = iterations;

            while (0 < remainingIterations) {
                for(int k = 0; k < antNumber; k++) {
                    var ant = new Ant(problem, pheromoneMatrix, costMatrix, pheromoneImportance, localSearchIterations, exploitationImportance);
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
                pheromoneMatrix.GlobalUpdate(bestSolution, problem);

                remainingIterations--;
            }

            // Apply pheromone conservation for the next problem
            pheromoneMatrix.Conserve();

            var convertedSolution = bestSolution.ConvertToDomainSolution();
            convertedSolution.ApplyMapping(problem.Mapping);
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine($"Best solution: {bestSolution.Cost}");
            Console.WriteLine(convertedSolution);

            return convertedSolution;
        }

        private double CalculateInitialPheromoneValue(Problem problem) {
            var heuristic = new GreedyHeuristic();
            return 1 / (problem.Requests.Length * heuristic.Solve(problem).Evaluate(problem));
        }

        /// <summary>
        /// Transforms the cost matrix of a problem to the form the ACS is using:
        /// 0: depot
        /// 1-n: current position of the vehicles
        /// n-m: requests
        /// </summary>
        /// <param name="problem"></param>
        /// <returns></returns>
        private long[,] TransformDistanceMatrix(Problem problem) {
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
    }
}
