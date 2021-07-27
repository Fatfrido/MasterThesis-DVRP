using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DVRP.Domain;
using Google.OrTools;
using Google.OrTools.ConstraintSolver;
using Google.Protobuf.WellKnownTypes;

namespace DVRP.Optimizer
{
    public class TabuSearch
    {
        public static Solution Solve(Problem problem) {
            // Create routing index manager
            RoutingIndexManager manager = 
                new RoutingIndexManager(problem.CostMatrix.GetLength(0), problem.VehicleCount, problem.Start, 
                                        Enumerable.Repeat(problem.Depot, problem.VehicleCount).ToArray());

            // Create routing model
            RoutingModel routing = new RoutingModel(manager);

            // Create and register transit callback
            int transitCallbackIndex = routing.RegisterTransitCallback((long fromIndex, long toIndex) => {
                var fromNode = manager.IndexToNode(fromIndex);
                var toNode = manager.IndexToNode(toIndex);
                return problem.CostMatrix[fromNode, toNode];
            });

            // Define cost of each edge
            routing.SetArcCostEvaluatorOfAllVehicles(transitCallbackIndex);

            //routing.SetArcCostEvaluatorOfVehicle(transitCallbackIndex, vehicle);
            
            // Get demand for each request
            var demands = new int[problem.Requests.Length + 1];
            problem.Requests.Select(r => r.Amount).ToArray().CopyTo(demands, 1);

            // Add capacity constraint
            int demandCallbackIndex = routing.RegisterUnaryTransitCallback((long fromIndex) => {
                // Convert from routing variable index to demand index
                var fromNode = manager.IndexToNode(fromIndex);
                return demands[fromNode];
            });

            long[] capacities = problem.VehicleCapacity.Select(x => (long) x).ToArray(); // convert to long[]
            routing.AddDimensionWithVehicleCapacity(demandCallbackIndex, 0, capacities, true, "Capacity");
            
            RoutingSearchParameters searchParameters =
                operations_research_constraint_solver.DefaultRoutingSearchParameters();
            // NOTE: first solution strategy is essential. with cheapest arc strategy this TSP would not be able to even solve easy problems
            // see: https://github.com/google/or-tools/issues/298
            searchParameters.FirstSolutionStrategy = FirstSolutionStrategy.Types.Value.ParallelCheapestInsertion;
            searchParameters.LocalSearchMetaheuristic = LocalSearchMetaheuristic.Types.Value.TabuSearch;
            searchParameters.TimeLimit = new Duration { Seconds = 1 };

            Assignment solution = routing.SolveWithParameters(searchParameters);
            return ConvertToSolution(solution, problem, manager, routing);
        }

        /// <summary>
        /// Converts the results of the TSP optimizer to a <see cref="Solution"/>
        /// </summary>
        /// <param name="assignment"></param>
        /// <param name="problem"></param>
        /// <param name="manager"></param>
        /// <param name="routing"></param>
        /// <returns></returns>
        private static Solution ConvertToSolution(in Assignment assignment, Problem problem, in RoutingIndexManager manager, in RoutingModel routing) {
            var solution = new Solution(problem.VehicleCount);

            for(int i = 0; i < problem.VehicleCount; i++) {
                var route = new List<int>();
                var index = routing.Start(i);

                while(!routing.IsEnd(index)) {
                    var node = manager.IndexToNode((int) index);

                    if(node != problem.Depot) {
                        route.Add(node);
                    }     
                    
                    index = assignment.Value(routing.NextVar(index));
                }
                solution.AddRoute(i, route.ToArray());
            }

            solution.ApplyMapping(problem.Mapping);

            // print solution after mapping
            Console.WriteLine(solution);

            return solution;
        }
    }
}
