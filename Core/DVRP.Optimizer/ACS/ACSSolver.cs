using DVRP.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace DVRP.Optimizer.ACS
{
    public class ACSSolver
    {
        private static double[,] pheromoneMatrix;
        private static double pheromoneEvaporation;

        public static DVRP.Domain.Solution Solve(Problem problem, int computationTime, int antNumber, double pheromoneEvaporation = 0.5, double pheromoneImportance = 0.5) {
            ACSSolver.pheromoneEvaporation = pheromoneEvaporation;
            
            // init pheromone level
            if(pheromoneMatrix == null) {
                // matrix needs to include dummy depots for each vehicle
                pheromoneMatrix = InitPheromoneMatrix(problem.Requests.Length + problem.VehicleCount);
            }

            Solution bestSolution = null;

            while(0 < computationTime) { // TODO computation time
                for(int k = 0; k < antNumber; k++) {
                    var ant = new Ant(problem, 0.2, pheromoneMatrix, pheromoneEvaporation, pheromoneImportance);
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
            Console.WriteLine($"Best solution: {bestSolution.Cost}");
            var convertedSolution = bestSolution.ConvertToDomainSolution();
            convertedSolution.ApplyMapping(problem.Mapping);
            Console.WriteLine(convertedSolution);

            return convertedSolution;
        }

        private static double[,] InitPheromoneMatrix(int locationNumber) {
            var matrix = new double[locationNumber, locationNumber];
            var initialPheromone = CalcInitialPheromone();

            for(int i = 0; i < locationNumber; i++) {
                for(int j = 0; j < locationNumber; j++) {
                    matrix[i, j] = initialPheromone;
                }
            }

            return matrix;
        }

        private static double CalcInitialPheromone() {
            return 0.2; // TODO
        }
    }
}
