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

        public static DVRP.Domain.Solution Solve(WorldState problem, int computationTime, int antNumber, double pheromoneEvaporation = 0.5) {
            ACSSolver.pheromoneEvaporation = pheromoneEvaporation;
            
            // init pheromone level
            if(pheromoneMatrix == null) {
                // matrix needs to include dummy depots for each vehicle
                pheromoneMatrix = InitPheromoneMatrix(problem.KnownRequests.Count + problem.VehicleCount);
            }

            Solution bestSolution = null;

            while(0 < computationTime) { // TODO computation time
                for(int k = 0; k < antNumber; k++) {
                    var ant = new Ant(problem, 0.2);
                    Console.WriteLine($"[Ant-{k}] FindSolution...");
                    var solution = ant.FindSolution();

                    if(bestSolution == null || solution.Cost < bestSolution.Cost) {
                        bestSolution = solution;
                        Console.WriteLine($"[Ant-{k}] Found new best solution");
                    }
                }

                // update global pheromone trail
                for(int i = 0; i < bestSolution.Route.Length - 1; i++) {
                    var from = bestSolution.Route[i];
                    var to = bestSolution.Route[i + 1];
                    
                    // update every edge between each node (customer)
                    pheromoneMatrix[from, to] =
                        (1 - pheromoneEvaporation) * pheromoneMatrix[from, to] +
                        pheromoneEvaporation / bestSolution.Cost;
                }

                computationTime--; // this is just temporary
            }

            return bestSolution.ConvertToDomainSolution();
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
